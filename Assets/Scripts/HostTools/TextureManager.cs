using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureManager : MonoBehaviour
{
    public GameObject texturePrefab;
    public bool createWhiteBackground = true;
    public string sortingLayerName = "Default";

    private GameObject[] spriteHolders;
    private Texture2D[] textures;

    public void InitializeTextures(int width, int height, int length, float ppu)
    {
        ppu = 1f / ppu;

        spriteHolders = new GameObject[length];
        textures = new Texture2D[length];
        for (int i = 0; i < length; i++)
        {
            textures[i] = new Texture2D(width, height, TextureFormat.ARGB32, false);
            textures[i].filterMode = FilterMode.Point;

            Color32[] pixels = new Color32[width * height];
            for (int x = 0; x < pixels.Length; x++) {
                pixels[x] = Color.clear;
            }
            textures[i].SetPixels32(pixels);
            textures[i].Apply();

            spriteHolders[i] = Instantiate(texturePrefab, transform);
            spriteHolders[i].name = ("Sorting Layer : " + (-4 + i));
            spriteHolders[i].GetComponent<MeshRenderer>().sortingLayerName = sortingLayerName;
            spriteHolders[i].GetComponent<MeshRenderer>().sortingOrder = -4 + i;
            spriteHolders[i].GetComponent<MeshRenderer>().material.mainTexture = textures[i];

            spriteHolders[i].transform.localPosition = new Vector3(width * ppu * 0.5f, height * ppu * 0.5f, 0);
            spriteHolders[i].transform.localScale = new Vector3(width * ppu, height * ppu, 1);
        }

        if(createWhiteBackground)
        {
            GameObject whiteBG = Instantiate(texturePrefab, transform);
            whiteBG.name = ("Background Sorting Layer : " + (-5));
            whiteBG.GetComponent<MeshRenderer>().sortingOrder = -5;

            whiteBG.transform.localPosition= new Vector3(width * ppu * 0.5f, height * ppu * 0.5f, 0);
            whiteBG.transform.localScale = new Vector3(width * ppu, height * ppu, 1);
        }
    }

    public void SetPixels(Color32[] newColors, int layer)
    {
        textures[layer].SetPixels32(newColors);
        textures[layer].Apply();
    }
}
