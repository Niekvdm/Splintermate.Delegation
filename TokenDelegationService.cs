using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using HiveAPI.CS;
using Splintermate.Connectors;
using Splintermate.Delegation.Models;
using Splintermate.Utilities;
using ILogger = Serilog.ILogger;

namespace Splintermate.Delegation
{
    public class TokenDelegationService
    {
        private readonly ProxyRotator _proxyRotator;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        private readonly SettingsConnector _settingsConnector;
        private readonly PlayersConnector _playersConnector;

        private readonly Account _account;

        public bool IsRunning = false;

        private CancellationTokenSource _cancellationTokenSource;

        public TokenDelegationService(ProxyRotator proxyRotator, IConfiguration configuration, ILogger logger)
        {
            _logger = logger;
            _proxyRotator = proxyRotator;
            _configuration = configuration;
            _account = new Account(configuration);

            _settingsConnector = new SettingsConnector(proxyRotator, logger);
            _playersConnector = new PlayersConnector(proxyRotator, logger);
        }

        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        private Task Stop()
        {
            IsRunning = false;

            return Task.CompletedTask;
        }

        public async Task Run(string[] players, string mode, int quantity, int threshold, string token)
        {
            IsRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var ct = _cancellationTokenSource.Token;

                await Task.Run(async () =>
                {
                    ct.ThrowIfCancellationRequested();

                    _logger.ForContext("Type", "Delegation").Information("{Username}> Starting token transfer", _account.Username);

                    players = players.Where(x => x != _account.Username).ToArray();

                    if (string.IsNullOrEmpty(_account.ActiveKey))
                    {
                        _logger.ForContext("Type", "Delegation").Error("{Username}> Active key is required for sending tokens", _account.Username);
                        return Stop();
                    }

                    if (!CBase58.ValidatePrivateWif(_account.ActiveKey))
                    {
                        _logger.ForContext("Type", "Delegation").Error("{Username}> Active key is invalid", _account.Username);
                        return Stop();
                    }

                    var settings = await _settingsConnector.Get();
                    var transactionsConnector = new TransactionsConnector(_account, settings, _proxyRotator, _logger);

                    var balances = await _playersConnector.GetBalances(_account.Username);

                    if (balances == null)
                    {
                        _logger.ForContext("Type", "Delegation").Error("{Username}> Failed to fetch player balances", _account.Username);
                        return Stop();
                    }

                    var hiveProperties = await transactionsConnector.HiveApi.get_dynamic_global_properties();
                    var hiveAccount = await transactionsConnector.HiveApi.find_rc_accounts(_account.Username);

                    if (hiveAccount == null)
                    {
                        _logger.ForContext("Type", "Delegation").Error("{Username}> Failed to fetch hive account", _account.Username);
                        return Stop();
                    }

                    var currentMana = hiveAccount.CalculateCurrentMana(General.GetHiveBlockTime(hiveProperties));
                    var percentageMana = hiveAccount.GetCurrentManaPercentage(General.GetHiveBlockTime(hiveProperties));

                    var totalRc = Convert.ToInt64(hiveAccount.MaxRc);
                    var initialRc = Convert.ToInt64(currentMana);

                    var initialBalance = balances.FirstOrDefault(balance => string.Equals(balance.Token, token, StringComparison.CurrentCultureIgnoreCase));

                    if (mode == "threshold")
                    {
                        var requiredBalance = quantity * players.Length;

                        if (initialBalance.Balance < requiredBalance)
                        {
                            _logger.ForContext("Type", "Delegation").Warning("{Username}> Possibly insufficient balance: {Balance}, Required: {Required}", _account.Username,
                                initialBalance.Balance, requiredBalance);
                            return Stop();
                        }
                    }

                    _logger.ForContext("Type", "Delegation").Information("---------------------");
                    _logger.ForContext("Type", "Delegation")
                        .Information("{Username}> {CurrentMana}/{MaxMana} RC {PercentageMana}%", _account.Username, initialRc, totalRc, percentageMana);
                    _logger.ForContext("Type", "Delegation").Information("{Username}> Mode: {Mode}", _account.Username, mode);
                    _logger.ForContext("Type", "Delegation").Information("{Username}> Token: {Token}", _account.Username, token);
                    _logger.ForContext("Type", "Delegation").Information("{Username}> Balance: {Balance}", _account.Username, initialBalance.Balance);

                    if (mode == "threshold")
                        _logger.ForContext("Type", "Delegation").Information("{Username}> Threshold: {Threshold}", _account.Username, threshold);

                    _logger.ForContext("Type", "Delegation").Information("{Username}> Quantity: {Quantity}", _account.Username, quantity);
                    _logger.ForContext("Type", "Delegation").Information("---------------------");

                    foreach (var player in players)
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                            ct.ThrowIfCancellationRequested();

                        _logger.ForContext("Type", "Delegation").Information("{Username}> Fetching player balances", player);

                        var c = await _playersConnector.GetBalances(player);

                        await Task.Delay(_configuration.GetValue<int?>("Delay") ?? 2500, ct);

                        if (c != null)
                        {
                            var tokenBalance = c.FirstOrDefault(balance => string.Equals(balance.Token, token, StringComparison.CurrentCultureIgnoreCase));

                            if (tokenBalance == null)
                            {
                                _logger.ForContext("Type", "Delegation").Warning("{Username}> Failed to get {Token} balance from player", player, token);
                                continue;
                            }

                            var balance = tokenBalance.Balance ?? 0;
                            var topupValue = quantity - balance;

                            if ((mode == "threshold" && tokenBalance.Balance > threshold) || (mode == "topup" && topupValue <= 0))
                            {
                                _logger.ForContext("Type", "Delegation").Information("{Username}> has enough {Token} remaining, {Balance}", player, token, tokenBalance.Balance);
                                continue;
                            }

                            var diffBalance = initialBalance.Balance - topupValue;

                            if (mode == "threshold")
                                diffBalance = initialBalance.Balance - quantity;


                            if (diffBalance <= 0)
                            {
                                _logger.ForContext("Type", "Delegation").Fatal("{Username}> insufficient balance: {Balance}, Required: {Required}, aborting operation",
                                    _account.Username,
                                    diffBalance,
                                    quantity
                                );
                                break;
                            }

                            if (mode == "threshold")
                            {
                                _logger.ForContext("Type", "Delegation").Information("{Username}> is below the {Limit} {Token}, {Balance} sending {Amount}", player, threshold,
                                    token,
                                    tokenBalance.Balance, quantity);
                            }
                            else if (mode == "topup")
                            {
                                _logger.ForContext("Type", "Delegation").Information("{Username}> is missing {Difference} {Token}, {Balance} sending {Amount}",
                                    player,
                                    topupValue,
                                    token,
                                    tokenBalance.Balance,
                                    topupValue
                                );
                            }

                            try
                            {
                                var result = await transactionsConnector.TransferTokens(player, (mode == "topup" ? topupValue : quantity), token.ToUpper());

                                if (result.Equals("RC"))
                                {
                                    _logger.ForContext("Type", "Delegation").Fatal("Insufficient resource credits remaining, aborting operation");
                                    break;
                                }

                                if (string.IsNullOrEmpty(result))
                                {
                                    _logger.ForContext("Type", "Delegation").Error("{Username}> Failed to send transaction to the Splinterlands API!", player);
                                    continue;
                                }

                                initialBalance.Balance -= quantity;
                            }
                            catch (Exception ex)
                            {
                                _logger.ForContext("Type", "Delegation").Error(ex, "{Username}> {Message}", player, ex.Message);
                            }
                        }
                    }

                    _logger.ForContext("Type", "Delegation").Information("Delegation of {Token} has finished", token);
                    
                    return Stop();
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.ForContext("Type", "Delegation").ForContext("Type", "Delegation").Error(ex, "Exception occured: {Message}", ex.Message);
                IsRunning = false;
            }
        }
    }
}