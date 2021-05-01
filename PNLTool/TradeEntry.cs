using DataTools.Text.Csv;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PNLTool
{

    public enum Side
    {
        Buy,
        Sell
    }

    public record TradeEntry
    {
        public DateTime Timestamp { get; set; }

        public string Symbol { get; set; }

        public Side? Side { get; set; } = null;

        public double PNL { get; set; }

        public TradeEntry()
        {

        }

        public TradeEntry(DateTime d, string symbol, double pnl, Side? s = null)
        {
            Timestamp = d;
            Symbol = symbol;
            Side = s;
            PNL = pnl;
        }

        public TradeEntry(CsvRow row)
        {
            var s = row["Closed Positions PNL"];

            s = s.Replace("USDT", "");

            float f;

            if (float.TryParse(s, out f))
            {
                PNL = f;
            }
            else
            {
                var i = s.LastIndexOf("USD");
                var n = 1;

                if (string.IsNullOrEmpty(s)) return;

                if (s[0] == '-') n = -1;

                if (i == -1) return;

                var j = s.LastIndexOf(" ", i);

                if (j == -1) return;

                s = s.Substring(j + 1, i - j - 1).Trim();

                s = s.Replace("USD", "");

                if (float.TryParse(s, out f))
                {
                    PNL += (f * n);

                }
            }

            var values = row.GetValues();

            Timestamp = DateTime.Parse(values[2]);
            Symbol = values[0];

        }
    }
}
