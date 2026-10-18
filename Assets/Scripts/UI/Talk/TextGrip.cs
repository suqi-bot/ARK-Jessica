using UnityEngine;
using UnityEngine.UI;

public class TextGrip : MonoBehaviour
{
    public Text textName;
    public Text textContect;

    public void Input(string contect)
    {
        textContect.text = contect;
    }

    public void Input(string contect, string name)
    {
        textName.text = name;
        textContect.text = contect;
    }

   
   
}
