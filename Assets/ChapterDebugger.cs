using System.Collections;
using UnityEngine;
using VRBuilder.Core;

public class ChapterDebugger : MonoBehaviour
{
    private IEnumerator Start()
    {
        while (ProcessRunner.Current == null)
        {
            Debug.Log("[ChapterDebugger] : Waiting for VR Builder process...");
            yield return null;
        }

        Debug.Log("[ChapterDebugger] : Process Loaded!");

        //foreach (var chapter in ProcessRunner.Current.Data.Chapters)
        //{
        //    Debug.Log("[ChapterDebugger] : TYPE = " + chapter.GetType());

        //    Debug.Log("[ChapterDebugger] : DATA = " + chapter.Data);

        //    Debug.Log("[ChapterDebugger] : DATA TYPE = " + chapter.Data.GetType());

        //    Debug.Log("[ChapterDebugger] : DATA JSON = " + JsonUtility.ToJson(chapter.Data));
        //}

        foreach (var chapter in ProcessRunner.Current.Data.Chapters)
        {
            Debug.Log("[ChapterDebugger] : ====================");

            var data = chapter.Data;
            var type = data.GetType();

            Debug.Log("[ChapterDebugger] : DATA TYPE = " + type.FullName);

            // Public Properties
            foreach (var property in type.GetProperties())
            {
                object value = null;

                try
                {
                    value = property.GetValue(data);
                }
                catch (System.Exception ex)
                {
                    value = "<ERROR : " + ex.Message + ">";
                }

                Debug.Log("[ChapterDebugger] : PROPERTY : "
                          + property.Name
                          + " = "
                          + value);
            }

            // Public Fields
            foreach (var field in type.GetFields())
            {
                object value = null;

                try
                {
                    value = field.GetValue(data);
                }
                catch (System.Exception ex)
                {
                    value = "<ERROR : " + ex.Message + ">";
                }

                Debug.Log("[ChapterDebugger] : FIELD : "
                          + field.Name
                          + " = "
                          + value);
            }
        }
    }
}