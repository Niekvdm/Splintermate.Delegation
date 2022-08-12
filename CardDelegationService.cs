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

        private readonly Account _account;


        public CardDelegationService(ProxyRotator proxyRotator, IConfiguration configuration, ILogger logger)
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
            
            if (!CBase58.ValidatePrivateWif(_account.PostingKey))
            {
                _logger.Error("{Username}> Posting key is invalid", _account.Username);
                return;
            }

            var settings = await _settingsConnector.Get();
            var transactionsConnector = new TransactionsConnector(_account, settings, _proxyRotator, _logger);

            var collection = await _playersConnector.GetCollection(_account.Username);

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

            _logger.Information("---------------------");
            _logger.Information("{Username}> {CurrentMana}/{MaxMana} RC {PercentageMana}", _account.Username, initialRc, totalRc, percentageMana);
            _logger.Information("{Username}> Cards: {Cards}", _account.Username, delegation.Cards.Length);
            _logger.Information("---------------------");

            _logger.Warning("Press <any key> to continue or <ctrl + c> to abort");
            Console.ReadKey();

            var delegationDict = new Dictionary<DelegationCard, Queue<CardDetails>>();

            var rcDict = new Dictionary<int, long>();

            foreach (var card in delegation.Cards)
            {
                var items = collection.Cards.Where(x =>
                        x.CardDetailId == card.Id &&
                        x.Gold == card.Gold &&
                        !x.IsForSale &&
                        !x.IsForRent &&
                        !x.IsDelegated &&
                        CompareBxc(x, card)
                    )
                    .ToList();

                if (items.Count > 0)
                {
                    _logger.Information("{Username}> #{CardId}: {Count} cards available", _account.Username, card.Id, items.Count);

                    delegationDict.Add(card, new Queue<CardDetails>(items));
                }
                else
                {
                    _logger.Information("{Username}> #{CardId}: No cards available!", _account.Username, card.Id);
                }
            }

            if (!delegationDict.Any())
            {
                _logger.Warning("{Username}> Nothing to delegate, aborting operation", _account.Username);
                return;
            }

            foreach (var player in delegation.Players)
            {
                _logger.Information("{Username}> Fetching card collection", player);
                var c = await _playersConnector.GetCollection(player);

                await Task.Delay(_configuration.GetValue<int?>("Delay") ?? 2500);

                if (c != null)
                {
                    var toDelegate = new List<CardDetails>();

                    foreach (var kvp in delegationDict)
                    {
                        var contains = c.Cards.Any(x => x.CardDetailId == kvp.Key.Id && x.Gold == kvp.Key.Gold && CompareBxc(x, kvp.Key) && x.Player == _account.Username);

                        if (contains)
                        {
                            continue;
                        }

                        if (kvp.Value.Count == 0)
                        {
                            _logger.Warning("{Username}> #{CardId}: Cards depleted!", player, kvp.Key.Id);
                            continue;
                        }

                        var card = kvp.Value.Dequeue();
                        toDelegate.Add(card);
                    }

                    if (toDelegate.Count > 0)
                    {
                        _logger.Information("{Username}> Delegating cards", player);
                        _logger.Information(string.Join(", ", toDelegate.Select(x => $"[{x.CardDetailId}] {x.Uid}")));

                        try
                        {
                            long? cost = rcDict.ContainsKey(toDelegate.Count) ? rcDict[toDelegate.Count] : null;

                            if (cost.HasValue && initialRc - cost.Value < 0)
                            {
                                _logger.Fatal("Insufficient resource credits remaining, aborting operation");
                                break;
                            }

                            var result = transactionsConnector.DelegateCards(player, toDelegate.Select(x => x.Uid).ToArray());

                            if (!rcDict.ContainsKey(toDelegate.Count))
                            {
                                await Task.Delay(5000);
                                var previous = Convert.ToInt64(hiveAccount.CalculateCurrentMana(General.GetHiveBlockTime(hiveProperties)));

                                hiveProperties = transactionsConnector.HiveApi.get_dynamic_global_properties();
                                hiveAccount = transactionsConnector.HiveApi.find_rc_accounts(_account.Username)?.ToObject<CHiveAccount>();

                                cost = Math.Abs(Convert.ToInt64(hiveAccount.CalculateCurrentMana(General.GetHiveBlockTime(hiveProperties))) - previous);

                                if (cost.Value > 0)
                                {
                                    rcDict.Add(toDelegate.Count, cost.Value);
                                }
                            }

                            if (!string.IsNullOrEmpty(result))
                            {
                                initialRc -= cost.Value;
                                _logger.Information("{Username}> Transaction {TransactionId} created!", player, result);
                                _logger.Information("[{InitialMana} / {TotalMana}] - {Cost} RC spent", initialRc, totalRc, cost.Value);
                            }
                            else
                            {
                                _logger.Error("{Username}> Failed to send transaction to the Splinterlands API!", player);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "{Username}> {Message}", player, ex.Message);
                        }
                    }
                }
                else
                {
                    _logger.Error("{Username}> Failed to fetch card collection!", player);
                }
            }

            _logger.Information("Delegation of cards has finished");
        }

        private bool CompareBxc(CardDetails left, DelegationCard right)
        {
            var alphaXp = Convert.ToInt32(left.AlphaXp ?? 0);

            if (right.Bcx == 1)
            {
                return alphaXp == 1 || left.Xp == 0 || left.Xp == 1;
            }

            return left.Xp == right.Bcx || alphaXp == right.Bcx;
        }
    }
}