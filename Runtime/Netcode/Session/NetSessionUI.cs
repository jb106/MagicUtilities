using UnityEngine;
using UnityEngine.UIElements;

namespace MagicUtilities.Netcode
{
    /// <summary>
    /// Plugs any UI Toolkit document (UIDocument or PanelRenderer) to a NetSession, by element names.
    /// Leave a name empty, or leave the element out of your UXML, and it is simply ignored.
    /// For uGUI or your own UI, skip this component and call NetSession directly.
    /// </summary>
    public class NetSessionUI : MonoBehaviour
    {
        [Tooltip("Leave empty to use NetSession.Instance.")]
        [SerializeField] private NetSession session;

        [Header("UI (one of them)")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private PanelRenderer panelRenderer;

        [Header("Element names")]
        [Tooltip("Shown while not connected.")]
        [SerializeField] private string offlinePanel = "ConnectionButtons";
        [Tooltip("Shown while connected.")]
        [SerializeField] private string onlinePanel = "InGameButtons";
        [SerializeField] private string hostButton = "HostButton";
        [SerializeField] private string joinButton = "JoinButton";
        [SerializeField] private string leaveButton = "LeaveButton";
        [Tooltip("TextField where the player types the code to join.")]
        [SerializeField] private string codeField = "CodeField";
        [Tooltip("Label showing the code to share.")]
        [SerializeField] private string codeLabel = "CodeLabel";
        [Tooltip("Label showing the state or the last error.")]
        [SerializeField] private string statusLabel = "StatusLabel";

        protected NetSession Session => session != null ? session : NetSession.Instance;

        protected VisualElement Root { get; private set; }

        private VisualElement _offlinePanel;
        private VisualElement _onlinePanel;
        private Button _hostButton;
        private Button _joinButton;
        private Button _leaveButton;
        private TextField _codeField;
        private Label _codeLabel;
        private Label _statusLabel;
        private NetSession _subscribed;

        protected virtual void OnEnable()
        {
            if (panelRenderer != null) panelRenderer.RegisterUIReloadCallback(OnUIReload);
            else if (uiDocument != null) Bind(uiDocument.rootVisualElement);

            SubscribeSession();
        }

        protected virtual void Start()
        {
            // The UIDocument may build its tree after our OnEnable
            if (Root == null && uiDocument != null) Bind(uiDocument.rootVisualElement);
            SubscribeSession();
            Refresh();
        }

        protected virtual void OnDisable()
        {
            if (panelRenderer != null) panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            Unbind();
            UnsubscribeSession();
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement rootElement, int version) => Bind(rootElement);

        // ------------------------------------------------------------------------------------------------

        /// <summary>Updates the UI from the session state. Override to change what is shown.</summary>
        protected virtual void Refresh()
        {
            if (Root == null) return;

            NetSession current = Session;
            NetSessionState state = current != null ? current.State : NetSessionState.Offline;
            bool connected = state == NetSessionState.Connected;
            bool idle = state == NetSessionState.Offline;

            SetVisible(_offlinePanel, connected == false);
            SetVisible(_onlinePanel, connected);

            _hostButton?.SetEnabled(idle);
            _joinButton?.SetEnabled(idle);
            _codeField?.SetEnabled(idle);
            _leaveButton?.SetEnabled(state == NetSessionState.Connected || state == NetSessionState.Connecting);

            if (_codeLabel != null) _codeLabel.text = current != null ? current.JoinCode : string.Empty;
            if (_statusLabel != null) _statusLabel.text = GetStatusText(current, state);
        }

        /// <summary>Text of the status label. Override to localize or change it.</summary>
        protected virtual string GetStatusText(NetSession current, NetSessionState state)
        {
            switch (state)
            {
                case NetSessionState.Connecting: return "Connecting...";
                case NetSessionState.Connected: return current.IsHost ? "Hosting" : "Connected";
                case NetSessionState.Disconnecting: return "Leaving...";
                default: return current != null ? current.LastReason : string.Empty;
            }
        }

        protected virtual void OnHostClicked() { if (Session != null) Session.Host(); }
        protected virtual void OnJoinClicked() { if (Session != null) Session.Join(_codeField != null ? _codeField.value : string.Empty); }
        protected virtual void OnLeaveClicked() { if (Session != null) Session.Leave(); }

        protected static void SetVisible(VisualElement element, bool visible)
        {
            if (element != null) element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ------------------------------------------------------------------------------------------------

        private void Bind(VisualElement root)
        {
            Unbind();
            if (root == null) return;

            Root = root;
            _offlinePanel = Find<VisualElement>(offlinePanel);
            _onlinePanel = Find<VisualElement>(onlinePanel);
            _hostButton = Find<Button>(hostButton);
            _joinButton = Find<Button>(joinButton);
            _leaveButton = Find<Button>(leaveButton);
            _codeField = Find<TextField>(codeField);
            _codeLabel = Find<Label>(codeLabel);
            _statusLabel = Find<Label>(statusLabel);

            if (_hostButton != null) _hostButton.clicked += OnHostClicked;
            if (_joinButton != null) _joinButton.clicked += OnJoinClicked;
            if (_leaveButton != null) _leaveButton.clicked += OnLeaveClicked;

            Refresh();
        }

        private void Unbind()
        {
            if (_hostButton != null) _hostButton.clicked -= OnHostClicked;
            if (_joinButton != null) _joinButton.clicked -= OnJoinClicked;
            if (_leaveButton != null) _leaveButton.clicked -= OnLeaveClicked;

            Root = null;
            _offlinePanel = _onlinePanel = null;
            _hostButton = _joinButton = _leaveButton = null;
            _codeField = null;
            _codeLabel = _statusLabel = null;
        }

        private T Find<T>(string elementName) where T : VisualElement =>
            string.IsNullOrEmpty(elementName) ? null : Root.Q<T>(elementName);

        private void SubscribeSession()
        {
            NetSession current = Session;
            if (current == null || current == _subscribed) return;

            UnsubscribeSession();
            _subscribed = current;
            current.StateChanged += OnStateChanged;
        }

        private void UnsubscribeSession()
        {
            if (_subscribed == null) return;

            _subscribed.StateChanged -= OnStateChanged;
            _subscribed = null;
        }

        private void OnStateChanged(NetSessionState state) => Refresh();
    }
}
