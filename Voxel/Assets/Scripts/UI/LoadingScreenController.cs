using System.Collections;
using After.Main;
using UnityEngine;

namespace VoxelWorld.UI
{
    public class LoadingScreenController : Controller
    {
        [Inject] private MyEventManager _eventManager;

        public CanvasGroup PanelCanvasGroup;
        public float FadeDuration = 0.3f;
        [Space]
        public GameObject SavedTextObject;
        public float SavedTextDuration = 3f;

        [Header("Intro")]
        public float IntroFadeDuration = 2f;
        
        private Coroutine _savedTextRoutine;
        private Coroutine _fadeRoutine;

        public override void Initialize()
        {
            base.Initialize();
            _eventManager.AddListener(EventName.OnWorldLoadStarted, HandleLoadStarted);
            _eventManager.AddListener(EventName.OnWorldLoadFinished, HandleLoadFinished);
            _eventManager.AddListener(EventName.OnWorldSaved, HandleWorldSaved);

            SavedTextObject.SetActive(false);

            PanelCanvasGroup.gameObject.SetActive(true);
            PanelCanvasGroup.alpha = 1f;
            _fadeRoutine = StartCoroutine(FadeTo(0f, IntroFadeDuration, () => PanelCanvasGroup.gameObject.SetActive(false)));
        }
        
        private void HandleWorldSaved()
        {
            if (_savedTextRoutine != null)
                StopCoroutine(_savedTextRoutine);

            _savedTextRoutine = StartCoroutine(ShowSavedTextRoutine());
        }

        private void HandleLoadStarted()
        {
            if (_fadeRoutine != null)
                StopCoroutine(_fadeRoutine);

            PanelCanvasGroup.gameObject.SetActive(true);
            _fadeRoutine = StartCoroutine(FadeTo(1f, FadeDuration));
        }

        private void HandleLoadFinished()
        {
            if (_fadeRoutine != null)
                StopCoroutine(_fadeRoutine);

            _fadeRoutine = StartCoroutine(FadeTo(0f, FadeDuration, () => PanelCanvasGroup.gameObject.SetActive(false)));
        }
        
        private IEnumerator ShowSavedTextRoutine()
        {
            SavedTextObject.SetActive(true);
            yield return new WaitForSeconds(SavedTextDuration);
            SavedTextObject.SetActive(false);
        }

        private IEnumerator FadeTo(float target, float duration, System.Action onComplete = null)
        {
            float start = PanelCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                PanelCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            PanelCanvasGroup.alpha = target;
            onComplete?.Invoke();
        }

        protected override void OnControllerDestroy()
        {
            base.OnControllerDestroy();
            if (_eventManager == null) 
                return;
            _eventManager.RemoveListener(EventName.OnWorldLoadStarted, HandleLoadStarted);
            _eventManager.RemoveListener(EventName.OnWorldLoadFinished, HandleLoadFinished);
            _eventManager.RemoveListener(EventName.OnWorldSaved, HandleWorldSaved);
        }
    }
}