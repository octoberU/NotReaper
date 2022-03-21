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
    }
}

