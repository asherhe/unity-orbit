using DG.Tweening;
using DG.Tweening.Core;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// indicates the time warp speed
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TimewarpIndicator : MonoBehaviour
    {
        private TMP_Text _text;

        Coroutine _fadeCoroutine;
        TweenerCore<float, float, DG.Tweening.Plugins.Options.FloatOptions> _fadeTween;

        /// <summary>
        /// seconds to wait before fading text out
        /// </summary>
        public float fadeDelay = 2.0f;
        /// <summary>
        /// seconds that the fade out takes
        /// </summary>
        public float fadeDuration = 1.0f;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            Universe.Instance.Timewarp.OnWarpChanged += ShowWarp;

            _fadeTween = DOTween.To(
                 () => _text.alpha,
                 a => _text.alpha = a,
                 0.0f,
                 fadeDuration
             ).SetAutoKill(false);
            _fadeTween.Complete();
            //_alphaTween.Pause();
        }

        private void ShowWarp()
        {
            _text.text = $"Time Warp: x{Universe.Instance.Timewarp.TimewarpScale}";
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(DelayFade());
        }

        /// <summary>
        /// wait a bit, and then fade (coroutine)
        /// </summary>
        private IEnumerator DelayFade()
        {
            _fadeTween.Rewind();
            yield return new WaitForSeconds(fadeDelay);
            _fadeTween.Play();
        }
    }
}
