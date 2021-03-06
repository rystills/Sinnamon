﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class drawPath : MonoBehaviour {
	public List<Vector2> points = new List<Vector2>();
	LineRenderer lineRenderer;
	bool drawing = false;
	public float playerGirth;
	public Transform debugPoint;
	float lineAlpha = .75f;
    GameObject gameManager;

	private void Start() {
		//initialie our lineRenderer with a green-red gradient
		lineRenderer = gameObject.GetComponent<LineRenderer>();
		Gradient gradient = new Gradient();
		gradient.SetKeys(
			new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
			new GradientAlphaKey[] { new GradientAlphaKey(lineAlpha, 0.0f), new GradientAlphaKey(lineAlpha, 1.0f) }
			);
		lineRenderer.colorGradient = gradient;
        gameManager = GameObject.Find("GameManager");
	}
	
	void Update () {
		if (Input.GetMouseButton(0) && gameManager.GetComponent<GameManager>().getShowPauseScreen() == false) {
			Collider2D hit;
			if (!drawing && Input.GetMouseButtonDown(0)) {
				//check if we clicked on a player unit; if so, start a new path at his position
				hit = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), 1 << 9);
				if (hit && hit.transform == transform.parent) {
					points.Clear();
					drawing = true;
				}
			}
			if (drawing) {
				Vector3 scaledMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				//force paths to stay within .1f of the camera
				scaledMousePos.x = Mathf.Max(Mathf.Min(scaledMousePos.x,10.3f), -10.3f);
				scaledMousePos.y = Mathf.Max(Mathf.Min(scaledMousePos.y,4.9f), -4.9f);

				if (points.Count == 0 || !(points[points.Count - 1].x == scaledMousePos.x && points[points.Count - 1].y == scaledMousePos.y)) {
					//check collisions before adding waypoint
					Vector2 oldPoint = points.Count > 0 ? points[points.Count - 1] : (Vector2)transform.parent.transform.position;
					Vector2 newPoint = scaledMousePos;
					RaycastHit2D rch;
					rch = Physics2D.CircleCast(oldPoint, playerGirth, (newPoint - oldPoint).normalized, Vector2.Distance(oldPoint, newPoint), (1 << 8) | (1 << 11));
					if (rch.collider == null) {
						//no collisions; add the point
						points.Add(new Vector2(scaledMousePos.x, scaledMousePos.y));
					}
					else {
						//translate collider point outside of collision, with a buffer of .01f to help with float rounding
						Vector2 finalPoint = rch.point;
						float ang = Mathf.Atan2((finalPoint.y - oldPoint.y), (finalPoint.x - oldPoint.x));
						finalPoint.x -= Mathf.Cos(ang) * (playerGirth);
						finalPoint.y -= Mathf.Sin(ang) * (playerGirth);
						//failsafe: move the point out in small additional increments until collision is resolved (should fix floating point imprecision issues)
						while (Physics2D.OverlapCircle(new Vector2(finalPoint.x,finalPoint.y), playerGirth, (1 << 8) | (1 << 11))) {
							finalPoint.x -= Mathf.Cos(ang) * (.001f);
							finalPoint.y -= Mathf.Sin(ang) * (.001f);
						}

                        //Let line drawer slide along walls
                        //Any extra collisions this causes seems to correct itself on the next loop
                        ang = Mathf.Atan2((scaledMousePos.y - oldPoint.y), (scaledMousePos.x - oldPoint.x));
                        if (ang < 0) ang += 2 * Mathf.PI;
						finalPoint.x += .01f * (ang < Mathf.PI / 2 || ang >= 3 * Mathf.PI / 2 ? 1 : -1);
						finalPoint.y += .01f * (ang < Mathf.PI ? 1 : -1);
                        points.Add(finalPoint);
                        //TODO: implement a final check to disallow points that ultimately result in a collision
                    }
				}
			}
		}
		else {
			drawing = false;
		}
		lineRenderer.positionCount = points.Count+1;
		lineRenderer.SetPosition(0, transform.parent.transform.position);
		for (int i = 0; i < points.Count; ++i) {
			lineRenderer.SetPosition(i+1, points[i]);
		}
	}
}
