using TargetPreview.Display;
using UnityEditor;

namespace TargetPreview.Editor
{
    [CustomPropertyDrawer(typeof(TargetPool.TargetDictionary))]
    public class AnySerializableDictionaryPropertyDrawer : SerializableDictionaryPropertyDrawer {}
}