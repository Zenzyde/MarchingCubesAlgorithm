using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCubeMesher : MonoBehaviour
{
	[SerializeField] private Vector3 gridMax, gridNoiseOffset;
	[SerializeField] private float gridCellRadius;
	[SerializeField] private float cellVisibleThreshold;
	[SerializeField] private float gridNoiseScale;
	[SerializeField] private float gridHeight2D;
	[SerializeField] private float specificConfigRadius;
	[SerializeField] private CubeData.CubeConfiguration configuration = CubeData.CubeConfiguration.perlinNoise;
	[SerializeField] private bool inverseTriangles;
	[SerializeField] private int morphingRange = 0;
	[SerializeField] private float cellUpdateStrength = .01f;

	private List<GridCell> gridCells = new List<GridCell>();

	private MeshFilter filter;
	private MeshCollider collider;

	private bool drawBoxBounds, drawGridCells;

	// Start is called before the first frame update
	void Start()
	{
		filter = GetComponent<MeshFilter>();
		collider = GetComponent<MeshCollider>();
		MarchingCubes.SetGridNoiseHeight2D(gridHeight2D);
		gridCells = MarchingCubes.CreateMarchingCubesGrid(transform.position, gridMax, gridNoiseOffset, gridCellRadius, gridNoiseScale, cellVisibleThreshold, configuration, specificConfigRadius);
		if (MarchingCubes.TryGetMesh(gridCells, out Mesh mesh))
		{
			filter.mesh = mesh;
			collider.sharedMesh = filter.mesh;
		}
	}

	void Update()
	{
		if (Input.GetMouseButton(0))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
			{
				if (MarchingCubes.TryUpdateGrid(ref gridCells, hit.point, true, cellUpdateStrength, morphingRange))
				{
					if (MarchingCubes.TryGetMesh(gridCells, out Mesh mesh))
					{
						filter.mesh = mesh;
						collider.sharedMesh = filter.mesh;
					}
				}
			}
		}
		else if (Input.GetMouseButton(1))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
			{
				if (MarchingCubes.TryUpdateGrid(ref gridCells, hit.point, false, cellUpdateStrength, morphingRange))
				{
					if (MarchingCubes.TryGetMesh(gridCells, out Mesh mesh))
					{
						filter.mesh = mesh;
						collider.sharedMesh = filter.mesh;
					}
				}
			}
		}

		if (Input.GetKeyDown(KeyCode.Alpha1))
			drawBoxBounds = !drawBoxBounds;
		if (Input.GetKeyDown(KeyCode.Alpha2))
			drawGridCells = !drawGridCells;
	}

	void OnDrawGizmos()
	{
		if (drawBoxBounds)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(transform.position, .15f);
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(gridMax, .15f);
			Gizmos.color = Color.white;

			HelperMethods.DrawBoxBounds(transform.position, transform.position + gridMax);
		}

		if (drawGridCells)
		{
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
	}
}
