using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HadoopCore.Scripts.Water {
    public class AutoResizeRT : MonoBehaviour {
        [Serializable]
        public class CameraRawImagePair {
            public Camera metaCamera;
            public RawImage metaRawImage;
        }

        public List<CameraRawImagePair> pairs = new List<CameraRawImagePair>();

        private Camera mainCamera;

        /**
         * 运行时不能再去修改 Game View 中的尺寸了，这会导致 RenderTexture 的尺寸不匹配
         */
        private void Start() {
            mainCamera = Camera.main;

            int width = Screen.width;
            int height = Screen.height;

            foreach (var pair in pairs) {
                if (pair.metaCamera == null || pair.metaRawImage == null) {
                    continue;
                }

                RenderTexture dynamicRT = new RenderTexture(width, height, 0);
                pair.metaCamera.targetTexture = dynamicRT;
                pair.metaRawImage.texture = dynamicRT;
            }
        }

        void LateUpdate() {
            foreach (var pair in pairs) {
                if (pair.metaCamera == null) continue;

                // 1. 同步缩放 (如果是2D正交相机)
                pair.metaCamera.orthographicSize = mainCamera.orthographicSize;

                // 如果你是3D透视相机，则同步 fieldOfView:
                // pair.waterCamera.fieldOfView = mainCamera.fieldOfView;

                // 2. 同步位置和旋转
                pair.metaCamera.transform.position = mainCamera.transform.position;
                pair.metaCamera.transform.rotation = mainCamera.transform.rotation;
            }
        }
    }
}