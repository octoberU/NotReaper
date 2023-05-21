using TargetPreview.Scripts.Targets;
using UnityEngine;
using TargetPreview.Targets;

namespace TargetPreview.Math
{
    public static class TargetTransform
    {
        public const int
            numRows = 7,
            numCols = 12,
            meleePitchBottomLeft = 98,
            meleePitchBottomRight = 99,
            meleePitchTopLeft = 100,
            meleePitchTopRight = 101;

        public const float
            degreesBetweenX = 17f,
            degreesBetweenY = 11f,
            distance = 15f,
            sphereOffsetY = -1.5f,
            sphereOffsetZ = 6f,
            meleeHeight = 1.82f,
            meleeHorizontalOffset = 0.35f,
            meleeVerticalOffset = 0.871f,
            meleeDepthOffset = 0.75f,
            meleeHeightDifference = 0.5f;


        public static TargetPosition CalculateTargetTransform(TargetCue cue) =>
            CalculateTargetTransform(cue.pitch, (cue.xOffset, cue.yOffset, cue.zOffset));

        public static TargetPosition CalculateTargetTransform(int pitch, (float x, float y, float z) offset)
        {
            if(pitch >= meleePitchBottomLeft && pitch <= meleePitchTopRight)
                return new TargetPosition(Quaternion.identity, GetMeleePosition(pitch).Add(offset));

            float column, row, zOffset;
            //Convert pitches to column and row.
            column = (pitch - ((pitch / numCols) * numCols));
            row = pitch/numCols;

            //Center the columns so that 0 is in the middle.
            column -= (float)(numCols -1 ) / 2f;
            row -= (float)(numRows - 1) / 2f;
            
            row += 1.5f;

            //Add the offset
            column += offset.x;
            row += offset.y;
            zOffset = distance + offset.z;

            //Convert to degrees
            Quaternion rotation = Quaternion.identity * Quaternion.Euler(row * degreesBetweenY * -1, column * degreesBetweenX, 0);
            Vector3 position = rotation * Vector3.forward * zOffset;
            
            position.y += sphereOffsetY;
            position.z += sphereOffsetZ;

            return new TargetPosition(rotation, position);
        }
        public static Vector3 Parabola(Vector3 start, Vector3 end, float height, float t)
        {
            var mid = Vector3.Lerp(start, end, t);

            return new Vector3(mid.x, GetHeight(t) + Mathf.Lerp(start.y, end.y, t), mid.z);
            
            float GetHeight(float x)
                => -4 * height * x * x + 4 * height * x;
        }

        public static Vector3 MeleeParabola(Vector3 start, Vector3 end, float height, float width, float t, float direction)
        {
            var mid = Vector3.Lerp(start, end, t);

            return new Vector3((GetWidth(t) + Mathf.Lerp(start.x, end.x, t)) * direction, (GetHeight(t) + Mathf.Lerp(start.y, end.y, t)), mid.z);

            float GetWidth(float x)
                => -4 * width * x * x + 4 * width * x;

            float GetHeight(float y)
                => -4 * height * y * y + 4 * height * y;
        }

        public static Vector3 GetMeleePosition(int pitch)
            => pitch switch
            {
                meleePitchBottomLeft => new Vector3(-meleeHorizontalOffset, meleeVerticalOffset - meleeHeightDifference, meleeDepthOffset),
                meleePitchBottomRight => new Vector3(meleeHorizontalOffset, meleeVerticalOffset - meleeHeightDifference, meleeDepthOffset),
                meleePitchTopLeft => new Vector3(-meleeHorizontalOffset, meleeVerticalOffset, meleeDepthOffset),
                meleePitchTopRight => new Vector3(meleeHorizontalOffset, meleeVerticalOffset, meleeDepthOffset),
                _ => throw new System.Exception("Invalid pitch")
            };
        
        public static Vector3 Add(this Vector3 input, (float x, float y, float z) offset) =>
            new(input.x + offset.x, input.y + offset.y, input.z + offset.z);
    }

}
