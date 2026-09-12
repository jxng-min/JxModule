using System.Collections.Generic;
using System.Linq;
using JxModule;
using UnityEngine;

namespace JxModule.Terminal
{
    public sealed class JxTerminalGUI : MonoBehaviour
    {
        private const string InputControlName = "JxTerminalInput";

        [BigHeader("Terminal")]
        [Header("State")]
        [SerializeField] private bool enableTerminal = true;
        [SerializeField] private bool visibleOnStart;

        [Space]
        [Header("Shortcut")]
        [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;

        [BigHeader("Layout")]
        [Header("Screen Rect")]
        [SerializeField, Range(0.2f, 1f)] private float widthRatio = 0.45f;
        [SerializeField, Range(0.15f, 1f)] private float heightRatio = 0.25f;
        [SerializeField] private Vector2 screenPadding = new(12f, 12f);

        [BigHeader("Typography")]
        [Header("Font")]
        [SerializeField] private Font font;
        [SerializeField] private int fontSize = 24;

        [BigHeader("Completion")]
        [Header("Candidates")]
        [SerializeField] private int maxCandidateCount = 8;

        [BigHeader("Colors")]
        [Header("Panels")]
        [SerializeField] private Color backgroundColor = new(0.03f, 0.035f, 0.045f, 0.94f);
        [SerializeField] private Color panelColor = new(0.08f, 0.09f, 0.11f, 0.96f);

        [Space]
        [Header("Input")]
        [SerializeField] private Color inputColor = new(0.11f, 0.12f, 0.15f, 1f);
        [SerializeField] private Color inputTextColor = Color.white;
        [SerializeField] private Color promptColor = new(0.7f, 0.9f, 1f);

        [Space]
        [Header("Completion")]
        [SerializeField] private Color candidateColor = new(0.12f, 0.13f, 0.16f, 0.96f);
        [SerializeField] private Color selectedCandidateColor = new(0.23f, 0.36f, 0.58f, 1f);

        private readonly JxTerminal terminal = new();
        private readonly JxTerminalHistory history = new();
        private readonly JxTerminalBuffer buffer = new();
        private readonly JxTerminalAutoComplete autoComplete = new();

        private bool _isVisible;
        private bool _shouldFocusInput;
        private bool _shouldScrollToBottom;
        private bool _shouldResetTextEditor;
        private string _input = string.Empty;
        private Vector2 _scrollPosition;
        private Rect _inputRowRect;
        private GUIStyle _backgroundStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _inputRowStyle;
        private GUIStyle _inputStyle;
        private GUIStyle _promptStyle;
        private GUIStyle _lineStyle;
        private GUIStyle _inputLineStyle;
        private GUIStyle _successLineStyle;
        private GUIStyle _warningLineStyle;
        private GUIStyle _errorLineStyle;
        private GUIStyle _candidateStyle;
        private GUIStyle _selectedCandidateStyle;
        private GUIStyle _candidateCountStyle;
        private Texture2D _backgroundTexture;
        private Texture2D _panelTexture;
        private Texture2D _inputTexture;
        private Texture2D _candidateTexture;
        private Texture2D _selectedCandidateTexture;

        public JxTerminal Terminal => terminal;
        public JxTerminalBuffer Buffer => buffer;
        public JxTerminalHistory History => history;
        public bool IsVisible => _isVisible;

        private void Awake()
        {
            _isVisible = visibleOnStart;
            _shouldFocusInput = _isVisible;
            _shouldScrollToBottom = true;
        }

        private void OnValidate()
        {
            ClearStyles();
        }

        private void OnDestroy()
        {
            ClearStyles();
        }

        private void OnGUI()
        {
            if (!enableTerminal)
            {
                return;
            }

            var currentEvent = Event.current;
            HandleToggle(currentEvent);

            if (!_isVisible)
            {
                return;
            }

            InitializeStyles();
            HandleKeyboard(currentEvent);
            DrawTerminal();
            FocusInputIfNeeded(currentEvent);
            ScrollToBottomIfNeeded(currentEvent);
        }

        public void Show()
        {
            _isVisible = true;
            _shouldFocusInput = true;
            _shouldScrollToBottom = true;
        }

        public void Hide()
        {
            _isVisible = false;
            autoComplete.Reset();
        }

        public void Toggle()
        {
            if (_isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private void HandleToggle(Event currentEvent)
        {
            if (currentEvent.type != EventType.KeyDown || currentEvent.keyCode != toggleKey)
            {
                return;
            }

            Toggle();
            currentEvent.Use();
        }

        private void HandleKeyboard(Event currentEvent)
        {
            if (currentEvent.type != EventType.KeyDown)
            {
                return;
            }

            if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
            {
                ExecuteInput();
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.UpArrow)
            {
                var previous = history.Previous();
                if (previous != null)
                {
                    SetInput(previous);
                }

                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.DownArrow)
            {
                var next = history.Next();
                if (next != null)
                {
                    SetInput(next);
                }

                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.Tab)
            {
                CompleteInput(currentEvent.shift);
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.Escape)
            {
                Hide();
                currentEvent.Use();
                return;
            }

            if (currentEvent.control && currentEvent.keyCode == KeyCode.L)
            {
                buffer.Clear();
                _shouldScrollToBottom = true;
                currentEvent.Use();
            }
        }

        private void DrawTerminal()
        {
            var width = Screen.width * widthRatio;
            var height = Screen.height * heightRatio;
            var rect = new Rect(
                screenPadding.x,
                Screen.height - height - screenPadding.y,
                width,
                height);

            GUILayout.BeginArea(rect, _backgroundStyle);
            DrawHeader();
            GUILayout.BeginVertical(_panelStyle, GUILayout.ExpandHeight(true));
            DrawBuffer();
            GUILayout.EndVertical();
            DrawInput();
            DrawCandidatesOverlay();
            GUILayout.EndArea();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Jx Terminal", _headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{terminal.GetCommandDescriptors().Count()} commands", _candidateCountStyle);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        private void DrawBuffer()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
            foreach (var entry in buffer.Entries)
            {
                GUILayout.Label(FormatEntry(entry), GetStyle(entry.Type));
            }

            GUILayout.EndScrollView();
        }

        private void DrawCandidatesOverlay()
        {
            if (!autoComplete.HasCandidates)
            {
                return;
            }

            var candidates = autoComplete.Candidates;
            var lineHeight = GetLineHeight();
            var availableHeight = Mathf.Max(0f, _inputRowRect.y - 6f);
            var maxVisibleLines = Mathf.FloorToInt((availableHeight - 16f) / lineHeight);
            if (maxVisibleLines <= 0)
            {
                return;
            }

            var count = Mathf.Min(candidates.Count, Mathf.Max(1, maxCandidateCount), maxVisibleLines);
            var moreLineCount = candidates.Count > count ? 1 : 0;
            if (count + moreLineCount > maxVisibleLines)
            {
                count = Mathf.Max(1, maxVisibleLines - 1);
                moreLineCount = candidates.Count > count ? 1 : 0;
            }

            var candidateHeight = 16f + (count + moreLineCount) * lineHeight;
            var candidateRect = new Rect(
                _inputRowRect.x,
                _inputRowRect.y - candidateHeight - 6f,
                _inputRowRect.width,
                candidateHeight);

            GUI.Box(candidateRect, GUIContent.none, _candidateStyle);

            var y = candidateRect.y + 8f;
            for (var i = 0; i < count; i++)
            {
                var style = i == autoComplete.CandidateIndex ? _selectedCandidateStyle : _lineStyle;
                var lineRect = new Rect(candidateRect.x + 10f, y, candidateRect.width - 20f, lineHeight);
                GUI.Label(lineRect, candidates[i], style);
                y += lineHeight;
            }

            if (candidates.Count > count)
            {
                var moreRect = new Rect(candidateRect.x + 10f, y, candidateRect.width - 20f, lineHeight);
                GUI.Label(moreRect, $"+ {candidates.Count - count} more", _candidateCountStyle);
            }
        }

        private void DrawInput()
        {
            GUILayout.Space(6f);
            var rowRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                _inputRowStyle,
                GUILayout.Height(GetInputHeight()),
                GUILayout.ExpandWidth(true));
            _inputRowRect = rowRect;

            GUI.Box(rowRect, GUIContent.none, _inputRowStyle);

            var promptWidth = GetLineHeight();
            var promptRect = new Rect(
                rowRect.x + 8f,
                rowRect.y,
                promptWidth,
                rowRect.height);
            var textFieldRect = new Rect(
                promptRect.xMax + 4f,
                rowRect.y,
                Mathf.Max(0f, rowRect.width - promptWidth - 20f),
                rowRect.height);

            GUI.Label(promptRect, ">", _promptStyle);
            GUI.SetNextControlName(InputControlName);
            ClampTextEditorState();
            var nextInput = GUI.TextField(textFieldRect, _input, _inputStyle);
            if (nextInput != _input)
            {
                _input = nextInput;
                RefreshAutoComplete();
                _shouldFocusInput = true;
            }
        }

        private void ExecuteInput()
        {
            if (string.IsNullOrWhiteSpace(_input))
            {
                SetInput(string.Empty);
                return;
            }

            var command = _input.Trim();
            buffer.AppendInput($"> {command}");
            history.Push(command);

            var result = terminal.Execute(command);
            buffer.AppendResult(result);

            SetInput(string.Empty);
            _shouldFocusInput = true;
            _shouldScrollToBottom = true;
        }

        private void CompleteInput(bool previous)
        {
            var cursorPosition = _input?.Length ?? 0;
            _input = autoComplete.Complete(terminal, _input, cursorPosition, previous, out _);
            _shouldFocusInput = true;
            _shouldResetTextEditor = true;
        }

        private void RefreshAutoComplete()
        {
            var cursorPosition = _input?.Length ?? 0;
            autoComplete.Refresh(terminal, _input, cursorPosition);
        }

        private void SetInput(string value)
        {
            _input = value ?? string.Empty;
            autoComplete.Reset();
            _shouldFocusInput = true;
            _shouldResetTextEditor = true;
        }

        private void FocusInputIfNeeded(Event currentEvent)
        {
            if (!_shouldFocusInput || currentEvent.type != EventType.Repaint)
            {
                return;
            }

            GUI.FocusControl(InputControlName);
            _shouldFocusInput = false;
        }

        private void ClampTextEditorState()
        {
            if (GUI.GetNameOfFocusedControl() != InputControlName)
            {
                return;
            }

            var textEditor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
            if (textEditor == null)
            {
                return;
            }

            var inputLength = _input?.Length ?? 0;
            if (_shouldResetTextEditor)
            {
                textEditor.cursorIndex = inputLength;
                textEditor.selectIndex = inputLength;
                _shouldResetTextEditor = false;
                return;
            }

            textEditor.cursorIndex = Clamp(textEditor.cursorIndex, 0, inputLength);
            textEditor.selectIndex = Clamp(textEditor.selectIndex, 0, inputLength);
        }

        private void ScrollToBottomIfNeeded(Event currentEvent)
        {
            if (!_shouldScrollToBottom || currentEvent.type != EventType.Repaint)
            {
                return;
            }

            _scrollPosition.y = float.MaxValue;
            _shouldScrollToBottom = false;
        }

        private void InitializeStyles()
        {
            if (_backgroundStyle != null)
            {
                return;
            }

            RebuildTextures();

            _backgroundStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _backgroundTexture },
                padding = new RectOffset(14, 14, 12, 14),
                border = new RectOffset(0, 0, 0, 0)
            };

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _panelTexture },
                padding = new RectOffset(10, 10, 8, 8),
                border = new RectOffset(0, 0, 0, 0)
            };

            _headerStyle = CreateLineStyle(new Color(0.9f, 0.96f, 1f));
            _headerStyle.fontStyle = FontStyle.Bold;

            _lineStyle = CreateLineStyle(new Color(0.85f, 0.85f, 0.85f));
            _inputLineStyle = CreateLineStyle(new Color(0.65f, 0.85f, 1f));
            _successLineStyle = CreateLineStyle(new Color(0.65f, 1f, 0.65f));
            _warningLineStyle = CreateLineStyle(new Color(1f, 0.85f, 0.4f));
            _errorLineStyle = CreateLineStyle(new Color(1f, 0.45f, 0.45f));
            _candidateStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _candidateTexture },
                padding = new RectOffset(10, 10, 8, 8),
                border = new RectOffset(0, 0, 0, 0)
            };
            _selectedCandidateStyle = CreateLineStyle(Color.white);
            _selectedCandidateStyle.normal.background = _selectedCandidateTexture;
            _selectedCandidateStyle.padding = new RectOffset(8, 8, 3, 3);
            _candidateCountStyle = CreateLineStyle(new Color(0.58f, 0.62f, 0.68f));
            _candidateCountStyle.fontSize = Mathf.Max(10, fontSize - 4);
            _inputRowStyle = CreateInputRowStyle();
            _inputStyle = CreateInputStyle();
            _promptStyle = CreateLineStyle(promptColor);
            _promptStyle.alignment = TextAnchor.MiddleCenter;
            _promptStyle.fixedHeight = GetInputHeight();
            _promptStyle.padding = new RectOffset(0, 0, 0, 0);
        }

        private GUIStyle CreateLineStyle(Color color)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = fontSize,
                wordWrap = false,
                richText = false,
                fixedHeight = GetLineHeight()
            };
            style.normal.textColor = color;
            return style;
        }

        private GUIStyle CreateInputStyle()
        {
            var style = new GUIStyle(GUI.skin.textField)
            {
                font = font,
                fontSize = fontSize,
                wordWrap = false,
                richText = false,
                fixedHeight = GetInputHeight(),
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 10, 0, 0),
                border = new RectOffset(0, 0, 0, 0)
            };
            style.normal.background = null;
            style.focused.background = null;
            style.hover.background = null;
            style.active.background = null;
            style.normal.textColor = inputTextColor;
            style.focused.textColor = inputTextColor;
            style.hover.textColor = inputTextColor;
            style.active.textColor = inputTextColor;
            return style;
        }

        private GUIStyle CreateInputRowStyle()
        {
            return new GUIStyle(GUI.skin.box)
            {
                normal = { background = _inputTexture },
                padding = new RectOffset(8, 8, 0, 0),
                border = new RectOffset(0, 0, 0, 0),
                fixedHeight = GetInputHeight()
            };
        }

        private float GetLineHeight()
        {
            return Mathf.Max(18, fontSize + 8);
        }

        private float GetInputHeight()
        {
            return GetLineHeight() + 12f;
        }

        private void RebuildTextures()
        {
            ClearTextures();
            _backgroundTexture = CreateTexture(backgroundColor);
            _panelTexture = CreateTexture(panelColor);
            _inputTexture = CreateTexture(inputColor);
            _candidateTexture = CreateTexture(candidateColor);
            _selectedCandidateTexture = CreateTexture(selectedCandidateColor);
        }

        private static Texture2D CreateTexture(Color color)
        {
            var texture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void ClearStyles()
        {
            _backgroundStyle = null;
            _panelStyle = null;
            _headerStyle = null;
            _inputRowStyle = null;
            _inputStyle = null;
            _promptStyle = null;
            _lineStyle = null;
            _inputLineStyle = null;
            _successLineStyle = null;
            _warningLineStyle = null;
            _errorLineStyle = null;
            _candidateStyle = null;
            _selectedCandidateStyle = null;
            _candidateCountStyle = null;
            ClearTextures();
        }

        private void ClearTextures()
        {
            DestroyTexture(_backgroundTexture);
            DestroyTexture(_panelTexture);
            DestroyTexture(_inputTexture);
            DestroyTexture(_candidateTexture);
            DestroyTexture(_selectedCandidateTexture);
            _backgroundTexture = null;
            _panelTexture = null;
            _inputTexture = null;
            _candidateTexture = null;
            _selectedCandidateTexture = null;
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private GUIStyle GetStyle(EJxTerminalBufferEntryType type)
        {
            return type switch
            {
                EJxTerminalBufferEntryType.Input => _inputLineStyle,
                EJxTerminalBufferEntryType.Success => _successLineStyle,
                EJxTerminalBufferEntryType.Warning => _warningLineStyle,
                EJxTerminalBufferEntryType.Error => _errorLineStyle,
                _ => _lineStyle
            };
        }

        private static string FormatEntry(JxTerminalBufferEntry entry)
        {
            return entry.Type switch
            {
                EJxTerminalBufferEntryType.Input => entry.Message,
                EJxTerminalBufferEntryType.Error => $"Error: {entry.Message}",
                EJxTerminalBufferEntryType.Warning => $"Warning: {entry.Message}",
                _ => entry.Message
            };
        }
    }
}
