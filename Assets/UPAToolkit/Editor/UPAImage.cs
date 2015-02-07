﻿//-----------------------------------------------------------------
// This class stores all information about the image.
// It has a full pixel map, width & height properties and some private project data.
// It also hosts functions for calculating how the pixels should be visualized in the editor.
//-----------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class UPAImage : ScriptableObject {

	// HELPER GETTERS
	private Rect window {
		get { return UPAEditorWindow.window.position; }
	}
	

	// IMAGE DATA

	public int width;
	public int height;

	public List<UPALayer> layers;

	public Texture2D finalImg;

	// VIEW & NAVIGATION SETTINGS

	[SerializeField]
	private float _gridSpacing = 20f;
	public float gridSpacing {
		get { return _gridSpacing + 1f; }
		set { _gridSpacing = Mathf.Clamp (value, 0, 140f); }
	}

	public float gridOffsetY = 0;
	public float gridOffsetX = 0;

	public int selectedLayer = 0;
	
	
	// PAINTING SETTINGS

	public Color selectedColor = new Color (1,0,0,1);
	public UPATool tool = UPATool.PaintBrush;
	public int gridBGIndex = 0;


	//MISC VARIABLES

	public bool dirty = false;		// Used for determining if data has changed
	
	
	// Class constructor
	public UPAImage () {
		// do nothing so far
	}

	// This is not called in constructor to have more control
	public void Init (int w, int h) {
		width = w;
		height = h;

		layers = new List<UPALayer>();
		UPALayer newLayer = new UPALayer (this);
		layers.Add ( newLayer );
		
		EditorUtility.SetDirty (this);
		dirty = true;
	}

	// Color a certain pixel by position in window in a certain layer
	public void SetPixelByPos (Color color, Vector2 pos, int layer) {
		Vector2 pixelCoordinate = GetPixelCoordinate (pos);

		if (pixelCoordinate == new Vector2 (-1, -1))
			return;

		Undo.RecordObject (layers[layer].tex, "ColorPixel");

		layers[layer].SetPixel ((int)pixelCoordinate.x, (int)pixelCoordinate.y, color);
		
		EditorUtility.SetDirty (this);
		dirty = true;
	}

	// Return a certain pixel by position in window
	public Color GetPixelByPos (Vector2 pos, int layer) {
		Vector2 pixelCoordinate = GetPixelCoordinate (pos);

		if (pixelCoordinate == new Vector2 (-1, -1)) {
			return Color.clear;
		} else {
			return layers[layer].GetPixel ((int)pixelCoordinate.x, (int)pixelCoordinate.y);
		}
	}

	public Color GetBlendedPixel (int x, int y) {
		SortLayersByOrder ();

		Color color = Color.clear;

		for (int i = 0; i < layers.Count; i++) {
			if (!layers[i].enabled)
				continue;

			Color pixel = layers[i].tex.GetPixel(x,y);

			// APPLY THE NORMAL COLOR BLENDING ALGORITHM

//			float newR = c.a * c.r + (1 - c.a) * bgColor.r;
//			c0 = Ca * Aa + Cb * Ab * (1 - Aa);

			float newR = pixel.r + color.r * (1 - pixel.a);
			float newG = pixel.g + color.g * (1 - pixel.a);
			float newB = pixel.b + color.b * (1 - pixel.a);
			float newA = pixel.a + color.a * (1 - pixel.a);

			color = new Color (newR, newG, newB, newA);
		}

		return color;
	}

	public void SortLayersByOrder () {
		layers = layers.OrderBy(layer => layer.order).ToList();
	}

	// Get the rect of the image as displayed in the editor
	public Rect GetImgRect () {
		float ratio = (float)height / (float)width;
		float w = gridSpacing * 30;
		float h = ratio * gridSpacing * 30;
		
		float xPos = window.width / 2f - w/2f + gridOffsetX;
		float yPos = window.height / 2f - h/2f + 20 + gridOffsetY;

		return new Rect (xPos,yPos, w, h);
	}

	public Vector2 GetPixelCoordinate (Vector2 pos) {
		Rect texPos = GetImgRect();
			
		if (!texPos.Contains (pos)) {
			return new Vector2(-1f,-1f);
		}

		float relX = (pos.x - texPos.x) / texPos.width;
		float relY = (texPos.y - pos.y) / texPos.height;
		
		int pixelX = (int)( width * relX );
		int pixelY = (int)( height * relY ) - 1;

		return new Vector2(pixelX, pixelY);
	}

	public Texture2D GetFinalImage (bool update) {

		if (!dirty && finalImg != null || !update && finalImg != null)
			return finalImg;

		finalImg = new Texture2D(width, height);
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				finalImg.SetPixel (x,y, GetBlendedPixel (x,y));
			}
		}
		finalImg.filterMode = FilterMode.Point;
		finalImg.Apply();

		dirty = false;
		return finalImg;
	}

	public void LoadAllTexsFromMaps () {
		for (int i = 0; i < layers.Count; i++) {
			if (layers[i].tex == null)
				layers[i].LoadTexFromMap();
		}
	}

	public void AddLayer () {
		Undo.RecordObject (this, "AddLayer");
		EditorUtility.SetDirty (this);
		this.dirty = true;

		UPALayer newLayer = new UPALayer (this);
		layers.Add(newLayer);
	}

	public void RemoveLayerAt (int index) {
		Undo.RecordObject (this, "RemoveLayer");
		EditorUtility.SetDirty (this);
		this.dirty = true;

		layers.RemoveAt (index);
		if (selectedLayer == index) {
			selectedLayer -= 1;
		}
	}
}
