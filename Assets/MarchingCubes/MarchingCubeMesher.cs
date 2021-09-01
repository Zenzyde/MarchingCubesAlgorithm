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
	[SerializeField] private bool march2D, inverseTriangles;

	private List<GridCell> gridCells = new List<GridCell>();

	private MeshFilter filter;

	private bool drawBoxBounds, drawGridCells;

	// Start is called before the first frame update
	void Start()
	{
		filter = GetComponent<MeshFilter>();
		MarchingCubes.SetMarch2D(march2D);
		MarchingCubes.SetGridNoiseHeight2D(gridHeight2D);
		gridCells = MarchingCubes.GetCreateGrid(transform.position, gridMax, gridNoiseOffset, gridCellRadius, gridNoiseScale, cellVisibleThreshold, configuration);
		if (MarchingCubes.TryGetMesh(gridCells, out Mesh mesh))
			filter.mesh = mesh;
	}

	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			MarchingCubes.SetNoiseOffset(gridNoiseOffset);
			MarchingCubes.SetGridMax(gridMax);
			MarchingCubes.SetCellVisibleThreshold(cellVisibleThreshold);
			MarchingCubes.SetGridRadius(gridCellRadius);
			MarchingCubes.SetInverseTriangles(inverseTriangles);
			MarchingCubes.SetNoiseScale(gridNoiseScale);
			MarchingCubes.SetSpecificConfigRadius(specificConfigRadius);
			MarchingCubes.SetMarch2D(march2D);
			MarchingCubes.SetGridNoiseHeight2D(gridHeight2D);
			if (MarchingCubes.GetUpdatedGrid(out List<GridCell> grid))
				gridCells = grid;
			if (MarchingCubes.TryGetMesh(gridCells, out Mesh mesh))
				filter.mesh = mesh;
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
