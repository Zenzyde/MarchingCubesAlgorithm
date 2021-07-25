using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeMesher : MonoBehaviour
{
	[SerializeField] private Vector3 gridMax, gridNoiseOffset;
	[SerializeField] private float gridCellRadius;
	[SerializeField] private float cellVisibleThreshold;
	[SerializeField] private float gridNoiseScale;
	[SerializeField] private float gridHeight2D;
	[SerializeField] private Material mat;
	[SerializeField] private CubeData.CubeConfiguration configuration = CubeData.CubeConfiguration.perlinNoise;
	[SerializeField] private bool march2D, inverseTriangles;

	private List<GridCell> gridCells = new List<GridCell>();
	private MarchingCube marchingCube;

	private Mesh mesh;
	private Renderer renderer;
	private MeshFilter filter;
	private GameObject go;
	private Transform t;

	// Start is called before the first frame update
	void Start()
	{
		// marchingCube = new MarchingCube(transform.position, gridMax, gridNoiseOffset, gridCellRadius, gridNoiseScale, cellVisibleThreshold, gridHeight2D, march2D, configuration);
		// renderer = GetComponent<Renderer>();
		// go = gameObject;
		// t = transform;
		// MarchCubes();
		// gridCells = marchingCube.GetGrid();

		filter = GetComponent<MeshFilter>();
		gridCells = MarchingCubes.GetCreateGrid(transform.position, gridMax, gridNoiseOffset, gridCellRadius, gridNoiseScale, cellVisibleThreshold, configuration);
		if (MarchingCubes.TryGetMesh(gridCells, out Mesh mesh))
			filter.mesh = mesh;
	}

	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			// marchingCube.SetGridRadius(gridCellRadius);
			// marchingCube.SetCellVisibleThreshold(cellVisibleThreshold);
			// marchingCube.SetNoiseOffset(gridNoiseOffset);
			// marchingCube.SetNoiseScale(gridNoiseScale);
			// marchingCube.SetInverseTriangles(inverseTriangles);
			// marchingCube.SetGridMin(transform.position);
			// marchingCube.SetGridMax(gridMax);
			// marchingCube.SetMarch2D(march2D);
			// MarchCubes();

			MarchingCubes.SetNoiseOffset(gridNoiseOffset);
			MarchingCubes.SetGridMax(gridMax);
			MarchingCubes.SetCellVisibleThreshold(cellVisibleThreshold);
			MarchingCubes.SetGridRadius(gridCellRadius);
			MarchingCubes.SetInverseTriangles(inverseTriangles);
			MarchingCubes.SetNoiseScale(gridNoiseScale);
			if (MarchingCubes.GetUpdatedGrid(out List<GridCell> grid))
				gridCells = grid;
			if (MarchingCubes.TryGetMesh(gridCells, out Mesh mesh))
				filter.mesh = mesh;
		}
	}

	void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, .15f);
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(gridMax, .15f);
		Gizmos.color = Color.white;

		Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, transform.position.y, gridMax.z));
		Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y, gridMax.z), new Vector3(gridMax.x, transform.position.y, gridMax.z));
		Gizmos.DrawLine(new Vector3(gridMax.x, transform.position.y, gridMax.z), new Vector3(gridMax.x, transform.position.y, transform.position.z));
		Gizmos.DrawLine(new Vector3(gridMax.x, transform.position.y, transform.position.z), transform.position);

		Gizmos.DrawLine(new Vector3(transform.position.x, gridMax.y, transform.position.z), new Vector3(transform.position.x, gridMax.y, gridMax.z));
		Gizmos.DrawLine(new Vector3(transform.position.x, gridMax.y, gridMax.z), new Vector3(gridMax.x, gridMax.y, gridMax.z));
		Gizmos.DrawLine(new Vector3(gridMax.x, gridMax.y, gridMax.z), new Vector3(gridMax.x, gridMax.y, transform.position.z));
		Gizmos.DrawLine(new Vector3(gridMax.x, gridMax.y, transform.position.z), new Vector3(transform.position.x, gridMax.y, transform.position.z));

		Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, gridMax.y, transform.position.z));
		Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y, gridMax.z), new Vector3(transform.position.x, gridMax.y, gridMax.z));
		Gizmos.DrawLine(new Vector3(gridMax.x, transform.position.y, gridMax.z), new Vector3(gridMax.x, gridMax.y, gridMax.z));
		Gizmos.DrawLine(new Vector3(gridMax.x, transform.position.y, transform.position.z), new Vector3(gridMax.x, gridMax.y, transform.position.z));

		foreach (GridCell cell in gridCells)
		{
			int index = 0;
			foreach (Vector3 pos in cell.pos)
			{
				index++;
				index %= 8;

				Gizmos.color = cell.val[index] < .5f ? Color.green : Color.red;
				Gizmos.DrawWireSphere(pos, .1f);
			}
		}
	}

	void MarchCubes()
	{
		mesh = marchingCube.GetMesh(ref go, ref mat);
		marchingCube.Reset();
		marchingCube.MarchCubes();
		marchingCube.SetMesh(ref t, ref mesh);
	}
}
