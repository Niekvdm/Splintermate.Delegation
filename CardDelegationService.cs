using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HiveAPI.CS;
using Splintermate.Connectors;
using Splintermate.Delegation.Models;
using Splintermate.Utilities;
using ILogger = Serilog.ILogger;

namespace Splintermate.Delegation
{
    public class CardDelegationService
    {
        private readonly ProxyRotator _proxyRotator;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        private readonly SettingsConnector _settingsConnector;
        private readonly PlayersConnector _playersConnector;
        private readonly CardsConnector _cardsConnector;

        private readonly Account _account;

        public bool IsRunning = false;

        private CancellationTokenSource _cancellationTokenSource;

        public CardDelegationService(ProxyRotator proxyRotator, IConfiguration configuration, ILogger logger)
        {
            _logger = logger;
            _proxyRotator = proxyRotator;
            _configuration = configuration;
            _account = new Account(configuration);

            _settingsConnector = new SettingsConnector(proxyRotator, logger);
            _cardsConnector = new CardsConnector(proxyRotator, logger);
            _playersConnector = new PlayersConnector(proxyRotator, logger);
        }

        private Task Stop()
        {
            IsRunning = false;

            return Task.CompletedTask;
        }
        
        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        public async Task Run(DelegationCard[] cards, string[] players)
        {
            IsRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var ct = _cancellationTokenSource.Token;

                await Task.Run(async () =>
                {
                    ct.ThrowIfCancellationRequested();

                    _logger.ForContext("Type", "Delegation").Information("{Username}> Starting card delegation", _account.Username);

                    players = players.Where(x => x != _account.Username).ToArray();

                    if (!CBase58.ValidatePrivateWif(_account.PostingKey))
                    {
                        _logger.ForContext("Type", "Delegation").Error("{Username}> Posting key is invalid", _account.Username);
                        return Stop();
                    }

                    var settings = await _settingsConnector.Get();
                    var allCards = await _cardsConnector.GetDetails();
                    var levelCalculator = new LevelCalculator(settings, allCards, _logger);

                    var transactionsConnector = new TransactionsConnector(_account, settings, _proxyRotator, _logger);

                    var collection = await _playersConnector.GetCollection(_account.Username);

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

                    _logger.ForContext("Type", "Delegation").Information("---------------------");
                    _logger.ForContext("Type", "Delegation")
                        .Information("{Username}> {CurrentMana}/{MaxMana} RC {PercentageMana}%", _account.Username, initialRc, totalRc, percentageMana);
                    _logger.ForContext("Type", "Delegation").Information("{Username}> Cards: {Cards}", _account.Username, cards.Length);
                    _logger.ForContext("Type", "Delegation").Information("---------------------");

                    //_logger.ForContext("Type", "Delegation").Warning("Press <any key> to continue or <ctrl + c> to abort");
                    //Console.ReadKey();

                    var delegationDict = new Dictionary<DelegationCard, Queue<CardDetails>>();

                    var rcDict = new Dictionary<int, long>();

                    foreach (var card in cards)
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                            ct.ThrowIfCancellationRequested();

                        var items = collection.Cards.Where(x =>
                                x.CardDetailId == card.Id &&
                                x.Gold == card.Gold &&
                                !x.IsForSale &&
                                !x.IsForRent &&
                                !x.IsDelegated &&
                                CompareLevel(levelCalculator, x, card)
                            )
                            .ToList();

                        if (items.Count > 0)
                        {
                            _logger.ForContext("Type", "Delegation").Information("{Username}> #{CardId}: {Count} cards available", _account.Username, card.Id, items.Count);

                            delegationDict.Add(card, new Queue<CardDetails>(items));
                        }
                        else
                        {
                            _logger.ForContext("Type", "Delegation").Information("{Username}> #{CardId}: No cards available!", _account.Username, card.Id);
                        }
                    }

                    if (!delegationDict.Any())
                    {
                        _logger.ForContext("Type", "Delegation").Warning("{Username}> Nothing to delegate, aborting operation", _account.Username);
                        return Stop();
                    }

                    foreach (var player in players)
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                            ct.ThrowIfCancellationRequested();

                        _logger.ForContext("Type", "Delegation").Information("{Username}> Fetching card collection", player);
                        var c = await _playersConnector.GetCollection(player);

                        await Task.Delay(_configuration.GetValue<int?>("Delay") ?? 2500, ct);

                        if (c != null)
                        {
                            var toDelegate = new List<CardDetails>();

                            foreach (var kvp in delegationDict)
                            {
                                var contains = c.Cards.Any(x =>
                                    x.CardDetailId == kvp.Key.Id && x.Gold == kvp.Key.Gold && CompareLevel(levelCalculator, x, kvp.Key) && x.Player == _account.Username);

                                if (contains)
                                {
                                    _logger.ForContext("Type", "Delegation").Information("{Username}> #{CardId}: Already has this card", player, kvp.Key.Id);

                                    continue;
                                }

                                if (kvp.Value.Count == 0)
                                {
                                    _logger.ForContext("Type", "Delegation").Warning("{Username}> #{CardId}: Cards depleted!", player, kvp.Key.Id);
                                    continue;
                                }

                                var card = kvp.Value.Dequeue();
                                toDelegate.Add(card);
                            }

                            if (toDelegate.Count > 0)
                            {
                                _logger.ForContext("Type", "Delegation").Information("{Username}> Delegating cards", player);
                                _logger.ForContext("Type", "Delegation").Information(string.Join(", ", toDelegate.Select(x => $"[{x.CardDetailId}] {x.Uid}")));

                                try
                                {
                                    long? cost = null;


                                    var result = await transactionsConnector.DelegateCards(player, toDelegate.Select(x => x.Uid).ToArray());

                                    if (result.Equals("RC"))
                                    {
                                        _logger.ForContext("Type", "Delegation").Fatal("Insufficient resource credits remaining, aborting operation");
                                        break;
                                    }

                                    await Task.Delay(5000, ct);

                                    hiveProperties = await transactionsConnector.HiveApi.get_dynamic_global_properties();
                                    hiveAccount = await transactionsConnector.HiveApi.find_rc_accounts(_account.Username);

                                    percentageMana = hiveAccount.GetCurrentManaPercentage(General.GetHiveBlockTime(hiveProperties));
                                    var current = Convert.ToInt64(hiveAccount.CalculateCurrentMana(General.GetHiveBlockTime(hiveProperties)));

                                    if (!string.IsNullOrEmpty(result))
                                    {
                                        _logger.ForContext("Type", "Delegation").Information("{Username}> Transaction {TransactionId} created!", player, result);
                                        _logger.ForContext("Type", "Delegation").Information("{Username}> {CurrentMana}/{MaxMana} RC {PercentageMana}%",
                                            _account.Username,
                                            current,
                                            totalRc,
                                            percentageMana
                                        );
                                    }
                                    else
                                    {
                                        _logger.ForContext("Type", "Delegation").Error("{Username}> Failed to send transaction to the Splinterlands API!", player);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.ForContext("Type", "Delegation").Error(ex, "{Username}> {Message}", player, ex.Message);
                                }
                            }
                        }
                        else
                        {
                            _logger.ForContext("Type", "Delegation").Error("{Username}> Failed to fetch card collection!", player);
                        }
                    }

                    _logger.ForContext("Type", "Delegation").Information("Delegation of cards has finished");

                    return Stop();
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.ForContext("Type", "Delegation").Error(ex, "Exception occured: {Message}", ex.Message);
                IsRunning = false;
            }
        }

        private bool CompareLevel(LevelCalculator levelCalculator, CardDetails left, DelegationCard right)
        {
            var level = levelCalculator.Calculate(left);

            return level == right.Level;

            // var alphaXp = Convert.ToInt32(left.AlphaXp ?? 0);
            //
            // if (right.Bcx == 1)
            // {
            //     return alphaXp == 1 || left.Xp == 0 || left.Xp == 1;
            // }
            //
            // return left.Xp == right.Bcx || alphaXp == right.Bcx;
        }
    }
}