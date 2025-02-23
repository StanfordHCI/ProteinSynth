using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.Rendering;

public class HidenSho : MonoBehaviour
{
    [SerializeField] lightType Ltype;
    [SerializeField] Renderer red, orange, yellow, green, blue, violet;
    enum colors {
        red, orange, yellow, green, blue, violet
    }

    enum lightType {
        emit, reflect, absorb
    }

    public void ShineWhite()
    {
        switch (Ltype){
            case lightType.emit:
                colors[] c = {colors.red,colors.orange,colors.yellow,colors.green,colors.blue,colors.violet};
                float[] f = {1f,1f,1f,1f,1f,1f};
                Show(c,f);
            break;
            case lightType.absorb:
                c = new colors[] {colors.red,colors.orange,colors.yellow,colors.green,colors.blue,colors.violet};
                f = new float[] {.9f, .2f,0,0f,1.1f,.7f};            
                Show(c,f);
            break;
            case lightType.reflect:
                c = new colors[] {colors.red,colors.orange,colors.yellow,colors.green,colors.blue,colors.violet};
                f = new float[] {1-.9f, 1-.2f,1-0,1-0f,1-1.1f,1-.7f};            
                Show(c,f);
            break;
        }

    }

    public void ShineBlue()
    {
        switch (Ltype){
            case lightType.emit:
                colors[] c = {colors.red,colors.orange,colors.yellow,colors.green,colors.blue,colors.violet};
                float[] f = {0,0,0,0,1f,0f};
                Show(c,f);
            break;
            case lightType.absorb:
                c = new colors[] {colors.red,colors.orange,colors.yellow,colors.green,colors.blue,colors.violet};
                f = new float[] {0, 0,0,0,1.1f,0};            
                Show(c,f);
            break;
            case lightType.reflect:
                c = new colors[] {colors.red,colors.orange,colors.yellow,colors.green,colors.blue,colors.violet};
                f = new float[] {0,0,0,0,-.1f,0};            
                Show(c,f);
            break;
        }
    }

    public void ShineRed()
    {
        switch (Ltype){
            case lightType.emit:
                colors[] c = {colors.red,colors.orange,colors.yellow,colors.green,colors.blue,colors.violet};
                float[] f = {1,0,0,0,0,0f};
                Show(c,f);
            break;
            case lightType.absorb:
                c = new colors[] {colors.red,colors.orange,colors.yellow,colors.green,colors.blue,colors.violet};
                f = new float[] {.90f,0,0,0,0f,0};            
                Show(c,f);
            break;
            case lightType.reflect:
                c = new colors[] {colors.red,colors.orange,colors.yellow,colors.green,colors.blue,colors.violet};
                f = new float[] {0.1f,0,0,0,0f,0};            
                Show(c,f);
            break;
        }
    }
     void Hide(colors[] c, float percent=1)
    {
        foreach (colors col in c)
        {
            switch (col)
            {
                            
                case colors.red:
                red.enabled = false;
                var v = red.transform.localScale;
                v.y = percent;
                red.transform.localScale = v;
                break;
                case colors.orange:
                orange.enabled = false;
                v = orange.transform.localScale;
                v.y = percent;
                orange.transform.localScale = v;
                break;
                case colors.yellow:
                yellow.enabled = false;
                v = yellow.transform.localScale;
                v.y = percent;
                yellow.transform.localScale = v;
                break;
                case colors.green:
                green.enabled = false;
                v = green.transform.localScale;
                v.y = percent;
                green.transform.localScale = v;
                break;
                case colors.blue:
                blue.enabled = false;
                v = blue.transform.localScale;
                v.y = percent;
                blue.transform.localScale = v;
                break;
                case colors.violet:
                violet.enabled = false;
                v = violet.transform.localScale;
                v.y = percent;
                violet.transform.localScale = v;                
                break;
                default:
                break;
            }
        }
    }
     void Show(colors[] c, float[] percent)
    {
        for (int i = 0; i < c.Length; i++)
        {
            switch (c[i])
            {

                case colors.red:
                    red.enabled = true;
                    var v = red.transform.parent.localScale;
                    if (Ltype == lightType.absorb)
                        v.z = percent[i];
                    else
                        v.y = percent[i];
                    red.transform.parent.localScale = v;
                    break;
                case colors.orange:
                    orange.enabled = true;
                    v = orange.transform.parent.localScale;
                    if (Ltype == lightType.absorb)
                        v.z = percent[i];
                    else
                        v.y = percent[i];
                    orange.transform.parent.localScale = v;
                    break;
                case colors.yellow:
                    yellow.enabled = true;
                    v = yellow.transform.parent.localScale;
                    if (Ltype == lightType.absorb)
                        v.z = percent[i];
                    else
                        v.y = percent[i];
                    yellow.transform.parent.localScale = v;
                    break;
                case colors.green:
                    green.enabled = true;
                    v = green.transform.parent.localScale;
                    if (Ltype == lightType.absorb)
                        v.z = percent[i];
                    else
                        v.y = percent[i];
                    green.transform.parent.localScale = v;
                    break;
                case colors.blue:
                    blue.enabled = true;
                    v = blue.transform.parent.localScale;
                    if (Ltype == lightType.absorb)
                        v.z = percent[i];
                    else
                        v.y = percent[i];
                    blue.transform.parent.localScale = v;
                    break;
                case colors.violet:
                    violet.enabled = true;
                    v = violet.transform.parent.localScale;
                    if (Ltype == lightType.absorb)
                        v.z = percent[i];
                    else
                        v.y = percent[i];
                    violet.transform.parent.localScale = v;
                    break;
                default:
                    break;
            }
        }
    }
}
