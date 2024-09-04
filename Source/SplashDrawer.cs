#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using UnityEngine.Networking;


	public class SplashDrawer
	{
        public Texture Splash;
        
        public bool showAs, hasSale, mcsSale, mcsCavesSale, deSale, tcSale, wcSale;
        public string saleText, mcsSaleText, mcsCavesSaleText, deSaleText, tcSaleText, wcSaleText;
        public string asUrl;
        public int waitSecondsForRecheck;

        float scrollBarX;
        UnityWebRequest www;

        public SplashDrawer()
        {
        }

        public static Type GetType(string typeName)
		{
			Type type = Type.GetType(typeName + ", Assembly-CSharp");
			if (type != null) return type;

			type = Type.GetType(typeName + ", Assembly-CSharp-firstpass");
			return type;
		}

        void UpdateSale()
        {
            if (www == null)
            {
                EditorApplication.update -= UpdateSale;
                SetPlayerPrefs();
                return;
            }

            if (!www.isDone) return;

            EditorApplication.update -= UpdateSale;
#if UNITY_2020_2_OR_NEWER
            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError ||
                www.result == UnityWebRequest.Result.DataProcessingError)
            {
                //Debug.Log(www.error);
                SetPlayerPrefs();
                return;
            }
#else
            if (www.isNetworkError || www.isHttpError)
            {
                //Debug.Log(www.error);
                SetPlayerPrefs();
                return;
            }
#endif
            string text = www.downloadHandler.text;
            string[] lines = text.Split('\n');

            // line 0 => seconds wait
            // line 1 => "No Sale" or `Sale Name`
            // line 2 => "AS__" or "AS_X" + AS URL

            int.TryParse(lines[0], out waitSecondsForRecheck);
            if (waitSecondsForRecheck < 10) waitSecondsForRecheck = 10;

            showAs = lines[2].Contains("AS__");
            if (showAs) asUrl = lines[2].Substring(5); 

            if (lines[1].Contains("No Sale"))
            {
                saleText = string.Empty;
                SetPlayerPrefs();
                return;
            }
            hasSale = true;
            saleText = lines[1];
            deSale = lines[3].Contains("DE_1");
            mcsSale = lines[4].Contains("MCS_1");
            mcsCavesSale = lines[5].Contains("MCSCaves_1");
            wcSale = lines[6].Contains("WC_1");
            tcSale = lines[7].Contains("TC_1");

            if (deSale) deSaleText = "50% Discount on Unity's " + saleText + " NOW!\n" + lines[3].Replace("DE_1 ", "") + "\n\n";
            if (mcsSale) mcsSaleText = "50% Discount on Unity's " + saleText + " NOW!\n" + lines[4].Replace("MCS_1 ", "") + "\n\n";
            if (mcsCavesSale) mcsCavesSaleText = "50% Discount on Unity's " + saleText + " NOW!\n" + lines[5].Replace("MCSCaves_1 ", "") + "\n\n";
            if (wcSale) wcSaleText = "50% Discount on Unity's " + saleText + " NOW!\n" + lines[6].Replace("WC_1 ", "") + "\n\n";
            if (tcSale) tcSaleText = "50% Discount on Unity's " + saleText + " NOW!\n" + lines[7].Replace("TC_1 ", "") + "\n\n";

            // Debug.Log(saleText + "de " + deSale + " mcs " + mcsSale + " mcsCaves " + mcsCavesSale + " wc " + wcSale + " tc " + tcSale);

            //for (int i = 0; i < lines.Length; i++)
            //{
            //    Debug.Log(lines[i]);
            //}
            SetPlayerPrefs();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        void SetPlayerPrefs()
        {
            PlayerPrefs.SetInt("PON_waitSecondsForRecheck", waitSecondsForRecheck);
            PlayerPrefs.SetString("PON_sale", saleText);
            PlayerPrefs.SetString("PON_deSale", deSaleText);
            PlayerPrefs.SetString("PON_mcsSale", mcsSaleText);
            PlayerPrefs.SetString("PON_mcsCavesSale", mcsCavesSaleText);
            PlayerPrefs.SetString("PON_tcSale", tcSaleText);
            PlayerPrefs.SetString("PON_wcSale", wcSaleText);
            PlayerPrefs.SetString("PON_asUrl", asUrl);
        }


        public void Draw(MonoBehaviour monoBehaviour)
        {

                string path = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(monoBehaviour)).Replace("ModMapManager.cs", "");

                Splash = AssetDatabase.LoadAssetAtPath(path + "Splash.png", typeof(Texture)) as Texture;
             
            Rect lastRect = GUILayoutUtility.GetLastRect();

            Rect rect = new Rect(lastRect.x, lastRect.y, 256, 128);
        

            if (GUI.Button(rect, Splash,GUIStyle.none))
            {
                Application.OpenURL("https://discord.gg/jwMhHzjtrB");
            }

            GUILayout.Space(rect.height);
            DrawSpacer(0, 5, 0);
        }




        static public void DrawSpacer(float spaceBegin = 5, float height = 5, float spaceEnd = 5)
        {
            GUILayout.Space(spaceBegin - 1);
            EditorGUILayout.BeginHorizontal();
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 1);
            GUILayout.Button(string.Empty, GUILayout.Height(height));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(spaceEnd - 1);

            GUI.color = Color.white;
        }
    }

#endif