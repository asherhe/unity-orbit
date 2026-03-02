using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class TextDisplay : MonoBehaviour
    {
        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            _text.text = GetText();
        }

        /// <summary>
        /// determines the text to display. override this to set custom text
        /// </summary>
        /// <returns></returns>
        protected virtual string GetText()
        {
            return "display string";
        }

        public static string AddMetricPrefix(double x)
        {
            double log10 = Math.Log10(Math.Abs(x));
            String prefix = "";
            if (log10 >= 9.0)
            {
                x /= 1e9;
                prefix = "G";
            }
            else if (log10 >= 6.0)
            {
                x /= 1e6;
                prefix = "M";
            }
            else if (log10 >= 3.0)
            {
                x /= 1e3;
                prefix = "k";
            }
            return String.Format("{0:F2}{1}", x, prefix);
        }

        public static string FormatDistance(double d)
        {
            return AddMetricPrefix(d) + "m";
        }

        public static string FormatSpeed(double v, bool showSign = false)
        {
            return string.Format($"{{0:{(showSign ? "+0.0;-0.0;0.0" : "F1")}}}<sprite name=\"mps\">", v);
        }

        public static string FormatTime(double t, bool showSign = false, bool shorten = false)
        {
            string sign = "";
            if (t < 0) sign = "-";
            else if (showSign) sign = "+";

            double absT = Math.Abs(t);
            int days = (int)(absT / 86400);
            int hours = (int)((absT % 86400) / 3600);
            int mins = (int)((absT % 3600) / 60);
            int secs = (int)(absT % 60);

            if (shorten)
            {
                if (days > 0 && hours + mins + secs == 0) return $"{days}d";
                if (hours > 0 && days + mins + secs == 0) return $"{hours}h";
                if (mins > 0 && days + hours + secs == 0) return $"{mins}m";
                if (secs > 0 && days + hours + mins == 0) return $"{secs}s";
            }

            string daysFormat = days > 0 ? $"{days}d " : "";
            return $"{sign}{daysFormat}{hours:D2}:{mins:D2}:{secs:D2}s";
        }
    }
}
