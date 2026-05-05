using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MicIndicatorUI : MonoBehaviour
{
    [SerializeField] private Sprite spriteTalking1;
    [SerializeField] private Sprite spriteTalking2;
    [SerializeField] private Sprite spriteEmpty;
    [SerializeField] private Sprite spriteMuted;

    private Image _image;
    private int _voiceId = -1;
    private bool _isLocal;
    private float _animTimer;
    private bool _frame1 = true;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    public void Init(int voiceId, bool isLocal)
    {
        _voiceId = voiceId;
        _isLocal = isLocal;
    }

    private void Update()
    {
        if (_image == null) return;

        bool muted, talking;

        if (_isLocal)
        {
            var vc = VoiceController.Instance;
            muted = vc != null && vc.IsLocalMuted;
            talking = vc != null && vc.IsLocalSpeaking;
        }
        else
        {
            muted = false;
            talking = _voiceId >= 0 && VoiceController.Instance != null
                      && VoiceController.Instance.IsPeerSpeaking(_voiceId);
        }

        if (muted)
        {
            _animTimer = 0f;
            _frame1 = true;
            _image.sprite = spriteMuted;
        }
        else if (talking)
        {
            _animTimer += Time.deltaTime;
            if (_animTimer >= 0.5f)
            {
                _animTimer -= 0.5f;
                _frame1 = !_frame1;
            }
            _image.sprite = _frame1 ? spriteTalking1 : spriteTalking2;
        }
        else
        {
            _animTimer = 0f;
            _frame1 = true;
            _image.sprite = spriteEmpty;
        }
    }
}
