using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JxModule
{
#region EDITOR
#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(JxShadowableObject))]
    public class ShadowableObjectEditor : Editor
    {
        private SerializedProperty lightPoint;
        private SerializedProperty hoverEnable;

        private SerializedProperty rootTransform;
        private SerializedProperty shadowTransform;
        private SerializedProperty rotationAxisTransform;
        private SerializedProperty shadowRenderer;

        private SerializedProperty baseShadowPositionOffset;
        private SerializedProperty hoverShadowPositionOffset;
        private SerializedProperty positionInterpolationSpeed;

        private SerializedProperty nearShadowAlpha;
        private SerializedProperty farShadowAlpha;
        private SerializedProperty alphaInterpolationSpeed;

        private void OnEnable()
        {
            hoverEnable = serializedObject.FindProperty("hoverEnable");
            
            rootTransform = serializedObject.FindProperty("rootTransform");
            shadowTransform = serializedObject.FindProperty("shadowTransform");
            rotationAxisTransform = serializedObject.FindProperty("rotationAxisTransform");
            shadowRenderer = serializedObject.FindProperty("shadowRenderer");
            
            baseShadowPositionOffset = serializedObject.FindProperty("baseShadowPositionOffset");
            hoverShadowPositionOffset = serializedObject.FindProperty("hoverShadowPositionOffset");
            positionInterpolationSpeed = serializedObject.FindProperty("positionInterpolationSpeed");
            
            nearShadowAlpha = serializedObject.FindProperty("nearShadowAlpha");
            farShadowAlpha = serializedObject.FindProperty("farShadowAlpha");
            alphaInterpolationSpeed = serializedObject.FindProperty("alphaInterpolationSpeed");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUIStyle headerStyle = new GUIStyle
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            var headerColor = Color.white;
            ColorUtility.TryParseHtmlString("#36FD96", out headerColor);
            headerStyle.normal.textColor = headerColor;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Shadowable Object", headerStyle);
            
            EditorGUILayout.Space(20);
            EditorGUILayout.PropertyField(hoverEnable);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Object References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(rootTransform);
            EditorGUILayout.PropertyField(shadowTransform);
            EditorGUILayout.PropertyField(rotationAxisTransform);
            EditorGUILayout.PropertyField(shadowRenderer);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Offset Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(baseShadowPositionOffset);

            if (hoverEnable.boolValue)
            {
                EditorGUILayout.PropertyField(hoverShadowPositionOffset);
                EditorGUILayout.PropertyField(positionInterpolationSpeed);
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Alpha Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(nearShadowAlpha);

            if (hoverEnable.boolValue)
            {
                EditorGUILayout.PropertyField(farShadowAlpha);
                EditorGUILayout.PropertyField(alphaInterpolationSpeed);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
#endregion EDITOR
    
    public class JxShadowableObject : MonoBehaviour
    {
        [SerializeField] private bool hoverEnable;
        
        [SerializeField] private Transform rootTransform;
        [SerializeField] private Transform shadowTransform;
        [SerializeField] private Transform rotationAxisTransform;
        [SerializeField] private SpriteRenderer shadowRenderer;
        
        [SerializeField] private float baseShadowPositionOffset = 0.02f;
        [SerializeField] private float hoverShadowPositionOffset = 0.14f;
        [SerializeField] private float positionInterpolationSpeed = 24f;
        
        [SerializeField] private float nearShadowAlpha = 0.75f;
        [SerializeField] private float farShadowAlpha = 0.5f;
        [SerializeField] private float alphaInterpolationSpeed = 4f;

        private Vector2 lightPoint;
        private int _baseSortingOrder;
        private bool _isHovering;
        
        private Vector3 _currentLocalPosition;
        private float _currentAlpha;
        
        private Vector3 _targetLocalPosition;
        private float _targetAlpha;
        
        private void Awake()
        {
            rootTransform ??= transform;

            if (shadowTransform == null)
            {
                Debug.LogError("Shadowable Object: shadow transform can not be null.");
                enabled = false;
                return;
            }

            if (shadowRenderer == null)
            {
                Debug.LogError("Shadowable Object: shadow renderer can not be null.");
                enabled = false;
                return;
            }

            _baseSortingOrder = shadowRenderer.sortingOrder;
            
            _currentAlpha = nearShadowAlpha;
            _targetAlpha = nearShadowAlpha;
        }

        private void Start()
        {
            if (JxVirtualLightPoint.Instance == null)
            {
                Debug.LogError("Shadowable Object: virtual shadow needs virtual light point.");
                enabled = false;
                return;
            }
            lightPoint = JxVirtualLightPoint.Instance.Position;
        }

        private void Update()
        {
            if (TryGetLightVector(rootTransform, out var lightVector))
            {
                UpdateShadowTarget(lightVector);
            }

            UpdateShadowRotation();
            UpdateShadowTransform();
            UpdateShadowAlpha();
        }

        public void SetHoverState(bool isHovering)
        {
            if (!hoverEnable)
            {
                return;
            }
            
            _isHovering = isHovering;
            _targetAlpha = _isHovering ? farShadowAlpha 
                                       : nearShadowAlpha;
        }

        public void ToggleRenderer(bool isActive)
        {
            shadowRenderer.enabled = isActive;
        }

        private void UpdateShadowRotation()
        {
            if (rotationAxisTransform == null)
            {
                return;
            }
            
            shadowTransform.localRotation = rotationAxisTransform.localRotation;
        }

        private void UpdateShadowTarget(Vector2 lightVector)
        {
            var normalizedLightVector = lightVector.normalized;
            var baseOffset = _isHovering ? hoverShadowPositionOffset
                                                : baseShadowPositionOffset;
            
            _targetLocalPosition = normalizedLightVector * baseOffset;

            shadowRenderer.sortingOrder = _isHovering ? _baseSortingOrder + 1
                                                      : _baseSortingOrder;
        }

        private void UpdateShadowTransform()
        {
            if (hoverEnable)
            {
                _currentLocalPosition = Vector3.Lerp(_currentLocalPosition, 
                                                     _targetLocalPosition, 
                                                     Time.deltaTime * positionInterpolationSpeed);               
            }
            else
            {
                _currentLocalPosition = _targetLocalPosition;
            }

            
            shadowTransform.localPosition = _currentLocalPosition;
        }

        private void UpdateShadowAlpha()
        {
            if (hoverEnable)
            {
                _currentAlpha = Mathf.Lerp(_currentAlpha,
                                           _targetAlpha,
                                           Time.deltaTime * alphaInterpolationSpeed);                
            }
            else
            {
                _currentAlpha = _targetAlpha;
            }
            
            SetShadowAlpha(_currentAlpha);
        }

        private void SetShadowAlpha(float alpha)
        {
            var color = shadowRenderer.color;
            color.a = alpha;
            shadowRenderer.color = color;
        }

        private bool TryGetLightVector(Transform targetTransform, out Vector2 lightVector)
        {
            lightVector = Vector2.zero;

            if (targetTransform == null)
            {
                return false;
            }
            
            var worldOffset = targetTransform.position - (Vector3)lightPoint;
            var localOffset = rootTransform.InverseTransformDirection(worldOffset);
            lightVector = new Vector2(localOffset.x, localOffset.y);
            
            return true;
        }
    }
}