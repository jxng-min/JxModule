using UnityEngine;
using UnityEngine.UI;

namespace JxModule
{
    public class JxShadowableUI : MonoBehaviour
    {
        [SerializeField] private Vector2 lightPoint = new(0.5f, 2f);

        [Header("UI References")]
        [SerializeField] private RectTransform rootRectTransform;
        [SerializeField] private RectTransform shadowRectTransform;
        [SerializeField] private RectTransform rotationAxisRectTransform;
        [SerializeField] private Graphic shadowGraphic;

        [Header("Offset References")]
        [SerializeField] private float baseShadowPositionOffset = 2f;
        [SerializeField] private float hoverShadowPositionOffset = 14f;
        [SerializeField] private float positionInterpolationSpeed = 24f;

        [Header("Alpha References")]
        [SerializeField] private float nearShadowAlpha = 0.75f;
        [SerializeField] private float farShadowAlpha = 0.5f;
        [SerializeField] private float alphaInterpolationSpeed = 4f;

        private int _baseSiblingIndex;

        private Vector2 _currentAnchoredPosition;
        private float _currentAlpha;

        private Vector2 _targetAnchoredPosition;
        private float _targetAlpha;

        private bool _isHovering;

        private void Awake()
        {
            rootRectTransform ??= transform as RectTransform;

            if (shadowRectTransform == null)
            {
                Debug.LogError("ShadowableUI: shadow rect transform can not be null.");
                enabled = false;
                return;
            }

            if (shadowGraphic == null)
            {
                Debug.LogError("ShadowableUI: shadow graphic can not be null.");
                enabled = false;
                return;
            }

            _baseSiblingIndex = shadowRectTransform.GetSiblingIndex();

            _currentAnchoredPosition = shadowRectTransform.anchoredPosition;
            _targetAnchoredPosition = _currentAnchoredPosition;

            _currentAlpha = nearShadowAlpha;
            _targetAlpha = nearShadowAlpha;

            SetShadowAlpha(_currentAlpha);
        }

        private void Update()
        {
            if (TryGetLightVector(rootRectTransform, out var lightVector))
            {
                UpdateShadowTarget(lightVector);
            }

            UpdateShadowRotation();
            UpdateShadowTransform();
            UpdateShadowAlpha();
        }

        public void SetHoverState(bool isHovering)
        {
            _isHovering = isHovering;
            _targetAlpha = _isHovering ? farShadowAlpha : nearShadowAlpha;
        }

        private void UpdateShadowRotation()
        {
            if (rotationAxisRectTransform == null)
            {
                return;
            }

            shadowRectTransform.localRotation = rotationAxisRectTransform.localRotation;
        }

        private void UpdateShadowTarget(Vector2 lightVector)
        {
            Vector2 normalizedLightVector = lightVector.normalized;
            float offset = _isHovering ? hoverShadowPositionOffset : baseShadowPositionOffset;

            _targetAnchoredPosition = normalizedLightVector * offset;

            if (_isHovering)
            {
                shadowRectTransform.SetSiblingIndex(Mathf.Min(_baseSiblingIndex + 1, shadowRectTransform.parent.childCount - 1));
            }
            else
            {
                shadowRectTransform.SetSiblingIndex(_baseSiblingIndex);
            }
        }

        private void UpdateShadowTransform()
        {
            _currentAnchoredPosition = Vector2.Lerp(
                _currentAnchoredPosition,
                _targetAnchoredPosition,
                Time.deltaTime * positionInterpolationSpeed
            );

            shadowRectTransform.anchoredPosition = _currentAnchoredPosition;
        }

        private void UpdateShadowAlpha()
        {
            _currentAlpha = Mathf.Lerp(
                _currentAlpha,
                _targetAlpha,
                Time.deltaTime * alphaInterpolationSpeed
            );

            SetShadowAlpha(_currentAlpha);
        }

        private void SetShadowAlpha(float alpha)
        {
            Color color = shadowGraphic.color;
            color.a = alpha;
            shadowGraphic.color = color;
        }

        private bool TryGetLightVector(RectTransform targetRectTransform, out Vector2 lightVector)
        {
            lightVector = Vector2.zero;

            if (targetRectTransform == null)
            {
                return false;
            }

            Vector2 rootWorldPosition = targetRectTransform.position;
            Vector2 worldOffset = rootWorldPosition - lightPoint;

            Vector3 localOffset3 = rootRectTransform.InverseTransformDirection(worldOffset);
            lightVector = new Vector2(localOffset3.x, localOffset3.y);

            return lightVector.sqrMagnitude > 0.0001f;
        }
    }
}