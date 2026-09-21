using UnityEngine;

namespace MixVerse.Fracture
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class FractureObjectView : MonoBehaviour
    {
        [SerializeField] private FractureShape _shape;
        [SerializeField, Range(2, 10)] private int _resolution = 6;
        private Mesh _mesh;
        private MaterialPropertyBlock _properties;
        private MeshRenderer _renderer;

        private void Awake()
        {
            _mesh = new FractureMeshFactory().Create(_shape, _resolution);
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = GetComponent<MeshRenderer>();
            _properties = new MaterialPropertyBlock();
        }

        public void SetProgress(float progress)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_properties);
            _properties.SetFloat("_Progress", Mathf.Clamp01(progress));
            _renderer.SetPropertyBlock(_properties);
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
