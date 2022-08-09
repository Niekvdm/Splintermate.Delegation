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


        public TokenDelegationService(ProxyRotator proxyRotator, IConfiguration configuration, ILogger logger)
        {
            _logger = logger;
            _proxyRotator = proxyRotator;
            _configuration = configuration;
            _account = new Account(configuration);

            _settingsConnector = new SettingsConnector(proxyRotator, logger);
            _playersConnector = new PlayersConnector(proxyRotator, logger);
        }

        public async Task Run()
        {
            var delegation = _configuration.GetSection("Delegation").Get<Models.Delegation>();

            if (string.IsNullOrEmpty(_account.ActiveKey))
            {
                _logger.Error("{Username}> Active key is required for sending tokens", _account.Username);
                return;
            }

            if (!CBase58.ValidatePrivateWif(_account.ActiveKey))
            {
                _logger.Error("{Username}> Active key is invalid", _account.Username);
                return;
            }

            var settings = await _settingsConnector.Get();
            var transactionsConnector = new TransactionsConnector(_account, settings, _proxyRotator, _logger);

            var balances = await _playersConnector.GetBalances(_account.Username);

            if (balances == null)
            {
                _logger.Error("{Username}> Failed to fetch player balances", _account.Username);
                return;
            }

            var token = delegation.Tokens.Token ?? "DEC";
            var threshold = delegation.Tokens.Threshold;
            var quantity = delegation.Tokens.Quantity;
            
            var hiveProperties = transactionsConnector.HiveApi.get_dynamic_global_properties();
            var hiveAccount = transactionsConnector.HiveApi.find_rc_accounts(_account.Username)?.ToObject<CHiveAccount>();
            
            if (hiveAccount == null)
            {
                _logger.Error("{Username}> Failed to fetch hive account", _account.Username);
                return;
            }

            var currentMana = hiveAccount.CalculateCurrentMana(General.GetHiveBlockTime(hiveProperties));
            var percentageMana = hiveAccount.GetCurrentManaPercentage(General.GetHiveBlockTime(hiveProperties));

            var totalRc = Convert.ToInt64(hiveAccount.MaxRc);
            var initialRc = Convert.ToInt64(currentMana);
            
            var initialBalance = balances.FirstOrDefault(balance => string.Equals(balance.Token, token, StringComparison.CurrentCultureIgnoreCase));
            var requiredBalance = quantity * delegation.Players.Length;

            if (initialBalance.Balance < requiredBalance)
            {
                _logger.Warning("{Username}> Possibly insufficient balance: {Balance}, Required: {Required}", _account.Username, initialBalance.Balance, requiredBalance);
                
                _logger.Warning("Press <any key> to continue or <ctrl + c> to abort");
                Console.ReadKey();
            }

            _logger.Information("---------------------");
            _logger.Information("{Username}> {CurrentMana}/{MaxMana} RC {PercentageMana}", _account.Username, initialRc, totalRc, percentageMana);
            _logger.Information("{Username}> Token: {Token}", _account.Username, token);
            _logger.Information("{Username}> Balance: {Balance}", _account.Username, initialBalance.Balance);
            _logger.Information("{Username}> Threshold: {Threshold}", _account.Username, threshold);
            _logger.Information("{Username}> Quantity: {Quantity}", _account.Username, quantity);
            _logger.Information("---------------------");

            _logger.Warning("Press <any key> to continue or <ctrl + c> to abort");
            Console.ReadKey();

            foreach (var player in delegation.Players)
            {
                _logger.Information("{Username}> Fetching player balances", player);
                
                var c = await _playersConnector.GetBalances(player);

                if (c != null)
                {
                    var tokenBalance = c.FirstOrDefault(balance => string.Equals(balance.Token, token, StringComparison.CurrentCultureIgnoreCase));

                    if (tokenBalance == null)
                    {
                        _logger.Warning("{Username}> Failed to get {Token} balance from player", player, token);
                        continue;
                    }

                    if (tokenBalance.Balance > threshold)
                    {
                        _logger.Verbose("{Username}> has enough {Token} remaining, {Balance}", player, token, tokenBalance.Balance);
                        continue;
                    }

                    if (initialBalance.Balance - quantity < 0)
                    {
                        _logger.Fatal("{Username}> insufficient balance: {Balance}, Required: {Required}, aborting operation", _account.Username, (initialBalance.Balance - quantity), quantity);
                        break;
                    }

                    _logger.Information("{Username}> is below the {Limit} {Token}, {Balance} sending {Amount}", player, threshold, token, tokenBalance.Balance, quantity);

                    try
                    {
                        var result = transactionsConnector.TransferTokens(player, quantity, token.ToUpper());

                        if (string.IsNullOrEmpty(result))
                        {
                            _logger.Error("{Username}> Failed to send transaction to the Splinterlands API!", player);
                            continue;
                        }

                        initialBalance.Balance -= quantity;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "{Username}> {Message}", player, ex.Message);
                    }
                }

                await Task.Delay(_configuration.GetValue<int?>("Delay") ?? 2500);
            }
            
            _logger.Information("Delegation of {Token} has finished", token);
        }
    }
}