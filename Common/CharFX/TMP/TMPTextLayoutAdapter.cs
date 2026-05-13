using System;
using TMPro;
using UnityEngine;

namespace JxModule.CharFX
{
    public class TMPTextLayoutAdapter : ICharFXTextLayout
    {
        private readonly TMP_Text _text;
        
        private TMP_TextInfo _textInfo;
        private Vector3[][] _baseVertices;
        private Vector3[][] _workVertices;

        public int CharacterCount => _textInfo?.characterCount ?? 0;
        
        public TMPTextLayoutAdapter(TMP_Text text)
        {
            _text = text;
        }

        public void Rebuild()
        {
            if (_text == null)
            {
                return;
            }
            
            _text.ForceMeshUpdate();
            _textInfo = _text.textInfo;

            var meshCount = _textInfo.meshInfo.Length;
            _baseVertices = new Vector3[meshCount][];
            _workVertices = new Vector3[meshCount][];

            for (var i = 0; i < meshCount; i++)
            {
                var source = _textInfo.meshInfo[i].vertices;
                
                _baseVertices[i] = new Vector3[source.Length];
                _workVertices[i] = new Vector3[source.Length];
                
                Array.Copy(source, _baseVertices[i], source.Length);
                Array.Copy(source, _workVertices[i], source.Length);
            }
        }

        public void ResetWorkToBase()
        {
            if (_baseVertices == null || _workVertices == null)
            {
                return;
            }

            for (var i = 0; i < _baseVertices.Length; i++)
            {
                if (_baseVertices[i] == null || _workVertices[i] == null)
                {
                    continue;
                }

                if (_baseVertices[i].Length != _workVertices[i].Length)
                {
                    continue;
                }
                
                Array.Copy(_baseVertices[i], _workVertices[i], _baseVertices[i].Length);
            }
        }

        public bool IsVisible(int charIndex)
        {
            if (_textInfo == null)
            {
                return false;
            }

            if (charIndex < 0 || charIndex >= CharacterCount)
            {
                return false;
            }
            
            return _textInfo.characterInfo[charIndex].isVisible;
        }

        public bool TryGetQuad(int charIndex, out CharQuad quad)
        {
            quad = default;

            if (_textInfo == null)
            {
                return false;
            }

            if (charIndex < 0 || charIndex >= CharacterCount)
            {
                return false;
            }

            var ch = _textInfo.characterInfo[charIndex];
            if (!ch.isVisible)
            {
                return false;
            }

            var materialIndex = ch.materialReferenceIndex;
            var vertexIndex = ch.vertexIndex;

            if (_workVertices == null)
            {
                return false;
            }

            if (materialIndex < 0 || materialIndex > _workVertices.Length)
            {
                return false;
            }
            
            var vertices = _workVertices[materialIndex];
            if (vertices == null)
            {
                return false;
            }

            if (vertexIndex < 0 || vertexIndex + 3 >= vertices.Length)
            {
                return false;
            }
            
            quad = new CharQuad(vertices[vertexIndex + 0],
                                vertices[vertexIndex + 1],
                                vertices[vertexIndex + 2],
                                vertices[vertexIndex + 3]);

            return true;
        }

        public void SetQuad(int charIndex, in CharQuad quad)
        {
            if (_textInfo == null)
            {
                return;
            }

            if (charIndex < 0 || charIndex >= CharacterCount)
            {
                return;
            }
            
            var ch = _textInfo.characterInfo[charIndex];
            if (!ch.isVisible)
            {
                return;
            }
            
            var materialIndex = ch.materialReferenceIndex;
            var vertexIndex = ch.vertexIndex;

            if (_workVertices == null)
            {
                return;
            }

            if (materialIndex < 0 || materialIndex > _workVertices.Length)
            {
                return;
            }
            
            var vertices =  _workVertices[materialIndex];
            if (vertices == null)
            {
                return;
            }

            if (vertexIndex < 0 || vertexIndex + 3 >= vertices.Length)
            {
                return;
            }

            vertices[vertexIndex + 0] = quad.V0;
            vertices[vertexIndex + 1] = quad.V1;
            vertices[vertexIndex + 2] = quad.V2;
            vertices[vertexIndex + 3] = quad.V3;
        }

        public void ApplyToText()
        {
            if (_text == null || _textInfo == null || _workVertices == null)
            {
                return;
            }

            for (var i = 0; i < _textInfo.meshInfo.Length; i++)
            {
                var meshInfo =  _textInfo.meshInfo[i];

                if (i < 0 || i >= _workVertices.Length)
                {
                    return;
                }
                
                meshInfo.mesh.vertices = _workVertices[i];
                _text.UpdateGeometry(meshInfo.mesh, i);
            }
        }
    }
}