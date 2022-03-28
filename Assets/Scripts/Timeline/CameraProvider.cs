using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper
{
    public class CameraProvider : MonoBehaviour
    {
        public static Camera main { get; private set; }
        public static Camera timeline { get; private set; }
        public static Camera menu { get; private set; }
        public static Camera grid { get; private set; }

        private void Awake()
        {
            main = Camera.main;
            timeline = GameObject.FindGameObjectWithTag("TimelineCamera").GetComponent<Camera>();
            menu = GameObject.FindGameObjectWithTag("MenuCamera").GetComponent<Camera>();
            grid = GameObject.FindGameObjectWithTag("GridCamera").GetComponent<Camera>();
        }

        public static void TargetPreviewMode()
        {
            main.enabled = false;
            timeline.enabled = false;
            menu.enabled = false;
            grid.enabled = false;
        }

        public static void ComposeMode()
        {
            main.enabled = true;
            timeline.enabled = true;
            menu.enabled = true;
            grid.enabled = true;
        }
    }
}

