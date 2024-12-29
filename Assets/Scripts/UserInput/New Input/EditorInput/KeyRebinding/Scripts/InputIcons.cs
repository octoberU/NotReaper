using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NotReaper.Keybinds
{
    public class InputIcons : MonoBehaviour
    {
        #region Icons
        #region Function Keys
        [Header("Function Keys")]
        [SerializeField] private Sprite f1;
        [SerializeField] private Sprite f2;
        [SerializeField] private Sprite f3;
        [SerializeField] private Sprite f4;
        [SerializeField] private Sprite f5;
        [SerializeField] private Sprite f6;
        [SerializeField] private Sprite f7;
        [SerializeField] private Sprite f8;
        [SerializeField] private Sprite f9;
        [SerializeField] private Sprite f10;
        [SerializeField] private Sprite f11;
        [SerializeField] private Sprite f12;
        #endregion

        #region Number Keys
        [Space, Header("Number Keys")]
        [SerializeField] private Sprite num0;
        [SerializeField] private Sprite num1;
        [SerializeField] private Sprite num2;
        [SerializeField] private Sprite num3;
        [SerializeField] private Sprite num4;
        [SerializeField] private Sprite num5;
        [SerializeField] private Sprite num6;
        [SerializeField] private Sprite num7;
        [SerializeField] private Sprite num8;
        [SerializeField] private Sprite num9;
        #endregion

        #region Letter Keys
        [Space, Header("Letter Keys")]
        [SerializeField] private Sprite letterA;
        [SerializeField] private Sprite letterB;
        [SerializeField] private Sprite letterC;
        [SerializeField] private Sprite letterD;
        [SerializeField] private Sprite letterE;
        [SerializeField] private Sprite letterF;
        [SerializeField] private Sprite letterG;
        [SerializeField] private Sprite letterH;
        [SerializeField] private Sprite letterI;
        [SerializeField] private Sprite letterJ;
        [SerializeField] private Sprite letterK;
        [SerializeField] private Sprite letterL;
        [SerializeField] private Sprite letterM;
        [SerializeField] private Sprite letterN;
        [SerializeField] private Sprite letterO;
        [SerializeField] private Sprite letterP;
        [SerializeField] private Sprite letterQ;
        [SerializeField] private Sprite letterR;
        [SerializeField] private Sprite letterS;
        [SerializeField] private Sprite letterT;
        [SerializeField] private Sprite letterU;
        [SerializeField] private Sprite letterV;
        [SerializeField] private Sprite letterW;
        [SerializeField] private Sprite letterX;
        [SerializeField] private Sprite letterY;
        [SerializeField] private Sprite letterZ;
        #endregion

        #region Modifier Keys
        [Space, Header("Modifier Keys")]
        [SerializeField] private Sprite ctrl;
        [SerializeField] private Sprite alt;
        [SerializeField] private Sprite shift;
        #endregion

        #region Arrow Keys
        [Space, Header("Arrow Keys")]
        [SerializeField] private Sprite arrowUp;
        [SerializeField] private Sprite arrowDown;
        [SerializeField] private Sprite arrowLeft;
        [SerializeField] private Sprite arrowRight;
        #endregion

        #region Operator Keys
        [Space, Header("Operator Keys")]
        [SerializeField] private Sprite opMinus;
        [SerializeField] private Sprite opSlash;
        [SerializeField] private Sprite opBackslash;
        [SerializeField] private Sprite opMarkLeft;
        [SerializeField] private Sprite opMarkRight;
        [SerializeField] private Sprite opMarkEquals;
        #endregion

        #region Misc Keys
        [Space, Header("Misc Keys")]
        [SerializeField] private Sprite miscEsc;
        [SerializeField] private Sprite miscEnter;
        [SerializeField] private Sprite miscCaps;
        [SerializeField] private Sprite miscDelete;
        [SerializeField] private Sprite miscBackspace;
        [SerializeField] private Sprite miscEnd;
        [SerializeField] private Sprite miscHome;
        [SerializeField] private Sprite miscInsert;
        [SerializeField] private Sprite miscPageDown;
        [SerializeField] private Sprite miscPageUp;
        [SerializeField] private Sprite miscSpace;
        [SerializeField] private Sprite miscTilda;
        [SerializeField] private Sprite miscQuote;
        [SerializeField] private Sprite miscBracketLeft;
        [SerializeField] private Sprite miscBracketRight;
        #endregion

        #region Mouse Buttons
        [Space, Header("Mouse Buttons")]
        [SerializeField] private Sprite mouseLeftClick;
        [SerializeField] private Sprite mouseRightClick;
        [SerializeField] private Sprite mouseMiddleClick;
        [SerializeField] private Sprite mouseScroll;
        [SerializeField] private Sprite mouseForward;
        [SerializeField] private Sprite mouseBack;
        #endregion

        [SerializeField] private Sprite noKey;

        #endregion

        public Sprite GetIcon(string path, out KeybindManager.Global.Modifiers modifier, out bool hasKey)
        {
            modifier = KeybindManager.Global.Modifiers.None;
            hasKey = true;

            if (string.IsNullOrEmpty(path))
            {
                hasKey = false;
                return noKey;
            }

            string origPath = path;
            path = path.Substring(path.IndexOf('/') + 1).ToLower();
            if (path.StartsWith("#("))
            {
                path = path.Replace("#", "");
                path = path.Replace("(", "");
                path = path.Replace(")", "");
            }

                     
            switch (path)
            {
                #region Function Keys
                case "f1":
                    return f1;
                case "f2":
                    return f2;
                case "f3":
                    return f3;
                case "f4":
                    return f4;
                case "f5":
                    return f5;
                case "f6":
                    return f6;
                case "f7":
                    return f7;
                case "f8":
                    return f8;
                case "f9":
                    return f9;
                case "f10":
                    return f10;
                case "f11":
                    return f11;
                case "f12":
                    return f12;
                #endregion

                #region Number Keys
                case "0":
                    return num0;
                case "1":
                    return num1;
                case "2":
                    return num2;
                case "3":
                    return num3;
                case "4":
                    return num4;
                case "5":
                    return num5;
                case "6":
                    return num6;
                case "7":
                    return num7;
                case "8":
                    return num8;
                case "9":
                    return num9;
                #endregion

                #region Letter Keys
                case "a":
                    return letterA;
                case "b":
                    return letterB;
                case "c":
                    return letterC;
                case "d":
                    return letterD;
                case "e":
                    return letterE;
                case "f":
                    return letterF;
                case "g":
                    return letterG;
                case "h":
                    return letterH;
                case "i":
                    return letterI;
                case "j":
                    return letterJ;
                case "k":
                    return letterK;
                case "l":
                    return letterL;
                case "m":
                    return letterM;
                case "n":
                    return letterN;
                case "o":
                    return letterO;
                case "p":
                    return letterP;
                case "q":
                    return letterQ;
                case "r":
                    return letterR;
                case "s":
                    return letterS;
                case "t":
                    return letterT;
                case "u":
                    return letterU;
                case "v":
                    return letterV;
                case "w":
                    return letterW;
                case "x":
                    return letterX;
                case "y":
                case "#(y)":
                    return letterY;
                case "z":
                case "#(z)":
                    return letterZ;
                #endregion

                #region Modifier Keys
                case "ctrl":
                case "leftctrl":
                case "rightctrl":
                    modifier = KeybindManager.Global.Modifiers.Ctrl;
                    return ctrl;
                case "alt":
                case "leftalt":
                case "rightalt":
                    modifier = KeybindManager.Global.Modifiers.Alt;
                    return alt;
                case "shift":
                case "leftshift":
                case "rightshift":
                    modifier = KeybindManager.Global.Modifiers.Shift;
                    return shift;
                #endregion

                #region Arrow Keys
                case "uparrow":
                    return arrowUp;
                case "downarrow":
                    return arrowDown;
                case "leftarrow":
                    return arrowLeft;
                case "rightarrow":
                    return arrowRight;
                #endregion

                #region Operator Keys
                case "minus":
                    return opMinus;
                case "slash":
                    return opSlash;
                case "backslash":
                    return opBackslash;
                case "comma":
                    return opMarkLeft;
                case "period":
                case "#(.)":
                case ".":
                    return opMarkRight;
                case "equals":
                    return opMarkEquals;
                #endregion

                #region Misc keys
                case "escape":
                    return miscEsc;
                case "enter":
                    return miscEnter;
                case "capslock":
                    return miscCaps;
                case "delete":
                    return miscDelete;
                case "backspace":
                    return miscBackspace;
                case "end":
                    return miscEnd;
                case "insert":
                    return miscInsert;
                case "home":
                    return miscHome;
                case "pageup":
                    return miscPageUp;
                case "pagedown":
                    return miscPageDown;
                case "space":
                    return miscSpace;
                case "tilda":
                    return miscTilda;
                case "quote":
                    return miscQuote;
                case "leftbracket":
                    return miscBracketLeft;
                case "rightbracket":
                    return miscBracketRight;
                case "leftbutton":
                    return mouseLeftClick;
                case "rightbutton":
                    return mouseRightClick;
                case "middlebutton":
                    return mouseMiddleClick;
                case "forwardbutton":
                    return mouseForward;
                case "backbutton":
                    return mouseBack;
                case "scroll/y":
                case "scroll":
                    return mouseScroll;
                #endregion
                default:
                    hasKey = false;
                    return noKey;
            }
        }
    }
}
