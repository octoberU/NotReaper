﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserPrefsManager : MonoBehaviour {

    public static Color leftColor { get; set; } = Color.blue;
    public static Color rightColor { get; set; } = Color.green;
    public static Color bothColor { get; set; } = Color.gray;
    public static Color neitherColor { get; set; } = Color.magenta;

    public static Color selectedColor { get; set; } = Color.yellow;

    public static float mouseHoldForDrag { get; set; } = 15f;


    //public Image LImage;
    //public Image RImage;

    public void Start() {
        //LImage.color = leftColor;
        //RImage.color = rightColor;
    }

    public static void ChangeColors() {
        //leftColor = Color.yellow;
    }


}