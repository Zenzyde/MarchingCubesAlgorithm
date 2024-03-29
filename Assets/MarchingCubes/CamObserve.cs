using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamObserve : MonoBehaviour
{
	[SerializeField] private float rotationSpeed, obervationDistance, yAxisOffset;
	[SerializeField] private ObservationAxis observationAxis;
	[SerializeField] private Transform observationPivot;

	bool noPivot = false;
	bool manualControlsEnabled = false;

	void Awake()
	{
		if (observationPivot == null)
		{
			Debug.LogError("No obervation pivot for observation camera! Assign a transform for use as pivot for observation camera!");
			noPivot = true;
			return;
		}

		transform.position = observationPivot.position;
		transform.rotation = Quaternion.LookRotation((observationPivot.position - transform.position).normalized);
		transform.position -= transform.forward * obervationDistance;
		transform.position += Vector3.up * yAxisOffset;
		transform.rotation = Quaternion.LookRotation((observationPivot.position - transform.position).normalized);
		transform.SetParent(observationPivot);
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Tab))
			manualControlsEnabled = !manualControlsEnabled;

		if (!manualControlsEnabled)
			return;

		if (Input.GetKey(KeyCode.E))
			transform.localPosition += transform.forward * rotationSpeed * Time.deltaTime;

		if (Input.GetKey(KeyCode.Q))
			transform.localPosition -= transform.forward * rotationSpeed * Time.deltaTime;

		if (Input.GetKey(KeyCode.W))
			observationPivot.Rotate(Vector3.right, rotationSpeed * Time.deltaTime, Space.Self);

		if (Input.GetKey(KeyCode.S))
			observationPivot.Rotate(Vector3.right, -rotationSpeed * Time.deltaTime, Space.Self);

		if (Input.GetKey(KeyCode.A))
			observationPivot.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

		if (Input.GetKey(KeyCode.D))
			observationPivot.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime, Space.World);
	}

	// Update is called once per frame
	void LateUpdate()
	{
		transform.rotation = Quaternion.LookRotation((observationPivot.position - transform.position).normalized);

		if (noPivot || manualControlsEnabled)
			return;

		switch ((int)observationAxis)
		{
			case 0:
				observationPivot.Rotate(Vector3.right, rotationSpeed * Time.deltaTime, Space.Self);
				break;
			case 1:
				observationPivot.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
				break;
			case 2:
				observationPivot.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
				break;
			case 3:
				observationPivot.Rotate(new Vector3(1, 1, 0), rotationSpeed * Time.deltaTime);
				break;
			case 4:
				observationPivot.Rotate(new Vector3(1, 0, 1), rotationSpeed * Time.deltaTime);
				break;
			case 5:
				observationPivot.Rotate(new Vector3(0, 1, 1), rotationSpeed * Time.deltaTime);
				break;
		}
	}

	enum ObservationAxis
	{
		X, Y, Z, XY, XZ, YZ
	}
}
