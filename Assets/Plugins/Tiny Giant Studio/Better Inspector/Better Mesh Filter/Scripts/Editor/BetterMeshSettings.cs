// Ignore Spelling: Gizmo

using UnityEditor;
using UnityEngine;

namespace TinyGiantStudio.BetterInspector
{
    /// <summary>
    /// This saves all user settings for Better Mesh Asset
    /// </summary>
    [FilePath("ProjectSettings/BetterInspector/BetterMeshSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class BetterMeshSettings : ScriptableSingleton<BetterMeshSettings>
    {
        public bool meshFieldOnTop = true;

        [SerializeField] private int _selectedUnit = 0;

        public int SelectedUnit
        {
            get { return _selectedUnit; }
            set
            {
                _selectedUnit = value;
                Save(true);
            }
        }

        #region Inspector Customization

        [SerializeField] private bool _overrideInspectorColor = false;

        public bool OverrideInspectorColor
        {
            get { return _overrideInspectorColor; }
            set
            {
                _overrideInspectorColor = value;
                Save(true);
            }
        }

        [SerializeField] private Color _inspectorColor = new Color(0, 0, 1, 0.025f);

        public Color InspectorColor
        {
            get { return _inspectorColor; }
            set
            {
                _inspectorColor = value;
                Save(true);
            }
        }

        [SerializeField] private bool _overrideFoldoutColor = false;

        public bool OverrideFoldoutColor
        {
            get { return _overrideFoldoutColor; }
            set
            {
                _overrideFoldoutColor = value;
                Save(true);
            }
        }

        [SerializeField] private Color _foldoutColor = new Color(0, 1, 0, 0.025f);

        public Color FoldoutColor
        {
            get { return _foldoutColor; }
            set
            {
                _foldoutColor = value;
                Save(true);
            }
        }

        #endregion Inspector Customization

        #region Mesh Preview

        [SerializeField] private bool _showMeshPreview = true;

        public bool ShowMeshPreview
        {
            get { return _showMeshPreview; }
            set
            {
                _showMeshPreview = value;
                Save(true);
            }
        }

        [SerializeField] private float _meshPreviewHeight = 200;

        public float MeshPreviewHeight
        {
            get { return _meshPreviewHeight; }
            set
            {
                _meshPreviewHeight = value;
                Save(true);
            }
        }

        [SerializeField] private Color _previewBackgroundColor = new Color(0.1764f, 0.1764f, 0.1764f);

        public Color PreviewBackgroundColor
        {
            get { return _previewBackgroundColor; }
            set
            {
                _previewBackgroundColor = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showAssetLocationBelowMesh = true;

        public bool ShowAssetLocationBelowMesh
        {
            get { return _showAssetLocationBelowMesh; }
            set
            {
                _showAssetLocationBelowMesh = value;
                Save(true);
            }
        }

        public bool runtimeMemoryUsageUnderPreview = true;
        public bool runtimeMemoryUsageInInformationFoldout;

        #endregion Mesh Preview

        [SerializeField] private bool _autoHideSettings = true;

        public bool AutoHideSettings
        {
            get { return _autoHideSettings; }
            set
            {
                _autoHideSettings = value;
                Save(true);
            }
        }

        #region Information List

        [SerializeField] private bool _showInformationFoldout = false;

        public bool ShowInformationFoldout
        {
            get { return _showInformationFoldout; }
            set
            {
                _showInformationFoldout = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showVertexInformation = true;

        public bool ShowVertexInformation
        {
            get { return _showVertexInformation; }
            set
            {
                _showVertexInformation = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showInformationOnPreview = true;

        public bool ShowInformationOnPreview
        {
            get { return _showInformationOnPreview; }
            set
            {
                _showInformationOnPreview = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showTriangleInformation = true;

        public bool ShowTriangleInformation
        {
            get { return _showTriangleInformation; }
            set
            {
                _showTriangleInformation = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showEdgeInformation = false;

        public bool ShowEdgeInformation
        {
            get { return _showEdgeInformation; }
            set
            {
                _showEdgeInformation = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showFaceInformation = false;

        public bool ShowFaceInformation
        {
            get { return _showFaceInformation; }
            set
            {
                _showFaceInformation = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showTangentInformation = true;

        public bool ShowTangentInformation
        {
            get { return _showTangentInformation; }
            set
            {
                _showTangentInformation = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showSizeFoldout = false;

        public bool ShowSizeFoldout
        {
            get { return _showSizeFoldout; }
            set
            {
                _showSizeFoldout = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showAssetLocationInFoldout = true;

        public bool ShowAssetLocationInFoldout
        {
            get { return _showAssetLocationInFoldout; }
            set
            {
                _showAssetLocationInFoldout = value;
                Save(true);
            }
        }

        #endregion Information List

        #region Gizmo

        [SerializeField] private bool _showDebugGizmoFoldout = true;

        public bool ShowDebugGizmoFoldout
        {
            get { return _showDebugGizmoFoldout; }
            set
            {
                _showDebugGizmoFoldout = value;
                Save(true);
            }
        }

        public bool useAntiAliasedGizmo = false;
        public int maximumGizmoDrawTime = 50;

        #endregion Gizmo

        #region Quick actions

        [SerializeField] private bool _doNotApplyActionToAsset = true;

        public bool DoNotApplyActionToAsset
        {
            get { return _doNotApplyActionToAsset; }
            set
            {
                _doNotApplyActionToAsset = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showActionsFoldout = true;

        public bool ShowActionsFoldout
        {
            get { return _showActionsFoldout; }
            set
            {
                _showActionsFoldout = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showOptimizeButton = true;

        public bool ShowOptimizeButton
        {
            get { return _showOptimizeButton; }
            set
            {
                _showOptimizeButton = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showRecalculateNormalsButton = true;

        public bool ShowRecalculateNormalsButton
        {
            get { return _showRecalculateNormalsButton; }
            set
            {
                _showRecalculateNormalsButton = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showRecalculateTangentsButton = false;

        public bool ShowRecalculateTangentsButton
        {
            get { return _showRecalculateTangentsButton; }
            set
            {
                _showRecalculateTangentsButton = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showFlipNormalsButton = true;

        public bool ShowFlipNormalsButton
        {
            get { return _showFlipNormalsButton; }
            set
            {
                _showFlipNormalsButton = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showGenerateSecondaryUVButton = false;

        public bool ShowGenerateSecondaryUVButton
        {
            get { return _showGenerateSecondaryUVButton; }
            set
            {
                _showGenerateSecondaryUVButton = value;
                Save(true);
            }
        }

        [SerializeField] private bool _showSaveMeshAsButton = true;

        public bool ShowSaveMeshAsButton
        {
            get { return _showSaveMeshAsButton; }
            set
            {
                _showSaveMeshAsButton = value;
                Save(true);
            }
        }

        #endregion Quick actions

        #region Reset

        public void ResetToDefault()
        {
            _showInformationFoldout = false;
            runtimeMemoryUsageInInformationFoldout = false;

            _showInformationOnPreview = true;
           runtimeMemoryUsageUnderPreview = true;

            _showSizeFoldout = true;
            _showAssetLocationBelowMesh = false;
            _showActionsFoldout = true;
            _showDebugGizmoFoldout = true;

            Reset();
        }

        public void ResetToDefault2()
        {
            _showInformationFoldout = true;
            runtimeMemoryUsageInInformationFoldout = true;
            _showAssetLocationInFoldout = true;

            _showInformationOnPreview = false;
           runtimeMemoryUsageUnderPreview = false;

            _showSizeFoldout = true;
            _showAssetLocationBelowMesh = false;
            _showActionsFoldout = true;
            _showDebugGizmoFoldout = true;

            Reset();
        }

        public void ResetToMinimal()
        {
            _showInformationFoldout = false;
            _showInformationOnPreview = true;
            _showSizeFoldout = false;
            _showAssetLocationBelowMesh = false;
            _showActionsFoldout = false;
            _showDebugGizmoFoldout = false;

            Reset();
        }

        public void ResetToNothing()
        {
            _showInformationFoldout = false;
            _showInformationOnPreview = false;
            _showSizeFoldout = false;
            _showAssetLocationBelowMesh = false;
            _showActionsFoldout = false;
            _showDebugGizmoFoldout = false;

            Reset();
        }

        private void Reset()
        {
            _selectedUnit = 0;

            _autoHideSettings = true;

            _showMeshPreview = true;
            _meshPreviewHeight = 200;

            _showVertexInformation = true;
            _showTriangleInformation = true;
            _showEdgeInformation = false;
            _showFaceInformation = false;
            _showTangentInformation = true;


            _showOptimizeButton = true;
            _showRecalculateNormalsButton = true;
            _showRecalculateTangentsButton = false;
            _showFlipNormalsButton = true;
            _showGenerateSecondaryUVButton = true;
            _showSaveMeshAsButton = true;

            _doNotApplyActionToAsset = true;

            _previewBackgroundColor = new Color(0.1764f, 0.1764f, 0.1764f);
            _overrideInspectorColor = false;
            _inspectorColor = new Color(0, 0, 1, 0.025f);
            _overrideFoldoutColor = false;
            _foldoutColor = new Color(0, 1, 0, 0.025f);

            Save();
        }

        #endregion Reset

        public void Save()
        {
            Save(true);
        }
    }
}