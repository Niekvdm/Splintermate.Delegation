using ILogger = Serilog.ILogger;

namespace Splintermate.Delegation
{
    public class LevelCalculator
    {
        private ILogger _logger;
        private Settings _settings;

        private IEnumerable<Card> _cards;

        public LevelCalculator(Settings settings, IEnumerable<Card> cards, ILogger logger)
        {
            _logger = logger;
            _settings = settings;

            _cards = cards;
        }

        public int Calculate(CardDetails item)
        {
            var card = _cards.FirstOrDefault(x => x.Id == item.CardDetailId);

            if (card == null)
            {
                _logger.Error("Card not found: #{CardDetailId} | {Edition}",
                    item.CardDetailId,
                    item.Edition
                );

                return 0;
            }

            return GetCardlevelInfo(item, card);
        }        

        private int GetCardlevelInfo(CardDetails details, Card card)
        {
            var xp = details.Xp;
            var level = 0;

            if (xp == 0)
            {
                xp = (details.Edition == 4 || card.Tier == 4) ? 1 : 0;
            }

            if (details.Edition == 4 || card.Tier >= 4)
            {
                var rates = details.Gold
                    ? _settings.CombineRatesGold[card.Rarity - 1]
                    : _settings.CombineRates[card.Rarity - 1];

                for (var i = 0; i < rates.Count; i++)
                {
                    if (rates[i] > xp)
                        break;

                    level++;
                }

                if (xp == 0)
                    level = 1;

                return level;
            }

            var levels = _settings.XpLevels[card.Rarity - 1];

            for (var i = 0; i < levels.Count; i++)
            {
                if (xp < levels[i])
                {
                    level = i + 1;
                    break;
                }
            }

            if (level == 0)
                level = levels.Count + 1;

            return level;
        }
    }
}