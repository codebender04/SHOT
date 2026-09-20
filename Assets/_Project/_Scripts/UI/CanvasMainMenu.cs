using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CanvasMainMenu : UICanvas
{
    [SerializeField] private TextMeshProUGUI txtTapAnywhereToStart;

    private Coroutine blinkCoroutine;

    private void OnEnable()
    {
        blinkCoroutine = StartCoroutine(BlinkText());
        GameManager.Instance.SetState(GameState.Pausing);
    }

    private void OnDisable()
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame ||
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame ||
            Gamepad.current != null && Gamepad.current.allControls.Count > 0)
        {
            RoundManager.Instance.StartRun();
            CloseImmediate();
        }
    }

    private IEnumerator BlinkText()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.8f);

            txtTapAnywhereToStart.CrossFadeAlpha(0f, 0.5f, true);

            yield return new WaitForSeconds(0.5f);

            txtTapAnywhereToStart.CrossFadeAlpha(1f, 0.5f, true);
        }
    }
}