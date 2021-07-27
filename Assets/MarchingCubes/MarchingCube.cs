using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MarchingCube
{
	public MarchingCube(Vector3 min, Vector3 max, Vector3 noiseOffset, float gridCellRadius, float gridNoiseScale, double cellVisibleThreshold,
		float gridHeight2D, bool march2D, CubeData.CubeConfiguration config = CubeData.CubeConfiguration.perlinNoise)
	{
		gridMin = min;
		gridMax = max;
		this.gridCellRadius = gridCellRadius;
		this.cellVisibleThreshold = cellVisibleThreshold;
		this.noiseOffset = noiseOffset;
		this.gridNoiseScale = gridNoiseScale;
		this.configuration = config;
		this.gridHeight2D = gridHeight2D;
		this.march2D = march2D;
	}

	private List<GridCell> grid = new List<GridCell>();
	private List<Vector3> vertices = new List<Vector3>();
	private List<int> triangles = new List<int>();
	private List<Vector2> uvs = new List<Vector2>();

	private Vector3 gridMin, gridMax, noiseOffset;
	private float gridCellRadius, gridNoiseScale, gridHeight2D;
	private double cellVisibleThreshold;
	private CubeData.CubeConfiguration configuration;
	private bool march2D = false;
	private bool inverseTriangles = false;

	public void Reset()
	{
		vertices.Clear();
		triangles.Clear();
		uvs.Clear();
		grid.Clear();
	}

	public void MarchCubes()
	{
		IEnumerator build = BuildGridCells();
		bool done = !build.MoveNext();
		while (!done)
		{
			done = !build.MoveNext();
		}
	}

	public void SetCubeVertex(Vector3 position, double value)
	{
		Vector3Int pos = position.ToVector3Int();
		GridCell cell = null;
		int valueIndex = -1;
		bool found = false;
		for (int i = 0; i < grid.Count; i++)
		{
			for (int j = 0; j < grid[i].pos.Length; j++)
			{
				if (grid[i].pos[j] == pos)
				{
					cell = grid[i];
					valueIndex = j;
					found = true;
				}
				if (found)
					break;
			}
			if (found)
				break;
		}

		cell.val[valueIndex] = value;

		Reset();
		int config = GetCubeConfig(cell);
		IsoFace(config, cell);
	}

	IEnumerator BuildGridCells()
	{
		Reset();

		for (float x = gridMin.x; x < gridMax.x; x += gridCellRadius)
		{
			for (float z = gridMin.z; z < gridMax.z; z += gridCellRadius)
			{
				for (float y = gridMin.y; y < gridMax.y; y += gridCellRadius)
				{
					if (x + gridCellRadius > gridMax.x || y + gridCellRadius > gridMax.y || z + gridCellRadius > gridMax.z)
						continue;
					GridCell cell = new GridCell();
					cell.pos = new Vector3[]
					{
						new Vector3(x, y, z + gridCellRadius), //LowFrontLeft
						new Vector3(x + gridCellRadius, y, z + gridCellRadius), //LowFrontRight
						new Vector3(x + gridCellRadius, y, z), //LowBackRight
						new Vector3(x, y, z), //LowBackLeft
						
						new Vector3(x, y + gridCellRadius, z + gridCellRadius), //HighFrontLeft
						new Vector3(x + gridCellRadius, y + gridCellRadius, z + gridCellRadius), //HighFrontRight
						new Vector3(x + gridCellRadius, y + gridCellRadius, z), //HighBackRight
						new Vector3(x, y + gridCellRadius, z), //HighBackLeft						
					};
					cell.val = new double[8];
					if (configuration == CubeData.CubeConfiguration.perlinNoise)
					{
						for (int i = 0; i < cell.val.Length; i++)
						{
							cell.val[i] = march2D ? GetNoiseValue2D(cell.pos[i], noiseOffset) : GetNoiseValue(cell.pos[i], noiseOffset);
						}
					}
					else if (configuration == CubeData.CubeConfiguration.sphere)
					{
						for (int i = 0; i < cell.val.Length; i++)
						{
							if (Vector3.Distance(cell.pos[i], (gridMin + gridMax) / 2f) < 2f)
								cell.val[i] = 1;
							else
								cell.val[i] = 0;
						}
					}
					grid.Add(cell);

					int config = GetCubeConfig(cell);

					if (CubeData.edgeTable[config] == 0)
						continue;

					IsoFace(config, cell);
					yield return null;
				}
			}
		}
	}

	int GetCubeConfig(GridCell cell)
	{
		// config code based on current cube on points
		int config = 0;

		config += cell.val[0] < cellVisibleThreshold ? (1 << 0) : 0; //alt: 0x0 (hex) or 0b0 (binary)
		config += cell.val[1] < cellVisibleThreshold ? (1 << 1) : 0; //alt: 0x1 or 0b1
		config += cell.val[2] < cellVisibleThreshold ? (1 << 2) : 0; //alt: 0x2 or 0b10
		config += cell.val[3] < cellVisibleThreshold ? (1 << 3) : 0; //alt: 0x3 or 0b11
		config += cell.val[4] < cellVisibleThreshold ? (1 << 4) : 0; //alt: 0x4 or 0b100
		config += cell.val[5] < cellVisibleThreshold ? (1 << 5) : 0; //alt: 0x5 or 0b101
		config += cell.val[6] < cellVisibleThreshold ? (1 << 6) : 0; //alt: 0x6 or 0b110
		config += cell.val[7] < cellVisibleThreshold ? (1 << 7) : 0; //alt: 0x7 or 0b111

		return config;
	}

	void IsoFace(int config, GridCell cell)
	{
		cell.vertices = new Vector3[12];

		//vertices
		int beforeCount = vertices.Count;
		/* Find the vertices where the surface intersects the cube */
		if ((CubeData.edgeTable[config] & 1) != 0)
			cell.vertices[0] = (InterpolateVerts(cellVisibleThreshold, cell.pos[0], cell.pos[1], cell.val[0], cell.val[1]));
		if ((CubeData.edgeTable[config] & 2) != 0)
			cell.vertices[1] = (InterpolateVerts(cellVisibleThreshold, cell.pos[1], cell.pos[2], cell.val[1], cell.val[2]));
		if ((CubeData.edgeTable[config] & 4) != 0)
			cell.vertices[2] = (InterpolateVerts(cellVisibleThreshold, cell.pos[2], cell.pos[3], cell.val[2], cell.val[3]));
		if ((CubeData.edgeTable[config] & 8) != 0)
			cell.vertices[3] = (InterpolateVerts(cellVisibleThreshold, cell.pos[3], cell.pos[0], cell.val[3], cell.val[0]));
		if ((CubeData.edgeTable[config] & 16) != 0)
			cell.vertices[4] = (InterpolateVerts(cellVisibleThreshold, cell.pos[4], cell.pos[5], cell.val[4], cell.val[5]));
		if ((CubeData.edgeTable[config] & 32) != 0)
			cell.vertices[5] = (InterpolateVerts(cellVisibleThreshold, cell.pos[5], cell.pos[6], cell.val[5], cell.val[6]));
		if ((CubeData.edgeTable[config] & 64) != 0)
			cell.vertices[6] = (InterpolateVerts(cellVisibleThreshold, cell.pos[6], cell.pos[7], cell.val[6], cell.val[7]));
		if ((CubeData.edgeTable[config] & 128) != 0)
			cell.vertices[7] = (InterpolateVerts(cellVisibleThreshold, cell.pos[7], cell.pos[4], cell.val[7], cell.val[4]));
		if ((CubeData.edgeTable[config] & 256) != 0)
			cell.vertices[8] = (InterpolateVerts(cellVisibleThreshold, cell.pos[0], cell.pos[4], cell.val[0], cell.val[4]));
		if ((CubeData.edgeTable[config] & 512) != 0)
			cell.vertices[9] = (InterpolateVerts(cellVisibleThreshold, cell.pos[1], cell.pos[5], cell.val[1], cell.val[5]));
		if ((CubeData.edgeTable[config] & 1024) != 0)
			cell.vertices[10] = (InterpolateVerts(cellVisibleThreshold, cell.pos[2], cell.pos[6], cell.val[2], cell.val[6]));
		if ((CubeData.edgeTable[config] & 2048) != 0)
			cell.vertices[11] = (InterpolateVerts(cellVisibleThreshold, cell.pos[3], cell.pos[7], cell.val[3], cell.val[7]));

		vertices.AddRange(cell.vertices);

		cell.triangles = new int[CubeData.triangleTable[config].Length * 3];
		for (int i = 0; CubeData.triangleTable[config][i] != -1; i += 3)
		{
			cell.triangles[i] = (CubeData.triangleTable[config][i]);
			cell.triangles[i + 1] = (CubeData.triangleTable[config][i + 1]);
			cell.triangles[i + 2] = (CubeData.triangleTable[config][i + 2]);
		}

		if (inverseTriangles)
		{
			for (int i = 0; i < cell.triangles.Length; i++)
			{
				triangles.Add(beforeCount + cell.triangles[i]);
			}
		}
		else
		{
			for (int i = cell.triangles.Length - 1; i >= 0; i--)
			{
				triangles.Add(beforeCount + cell.triangles[i]);
			}
		}

		//triangles + uvs
		int addedCount = vertices.Count - beforeCount;
		int addedIndex = 0;
		bool adc = true;
		for (int i = addedCount; i > 0; i--)
		{
			if (adc)
			{
				switch (addedIndex)
				{
					case 0:
						uvs.Add(UVCoord.A);
						break;
					case 1:
						uvs.Add(UVCoord.C);
						break;
					case 2:
						uvs.Add(UVCoord.D);
						break;
				}
			}
			else
			{
				switch (addedIndex)
				{
					case 0:
						uvs.Add(UVCoord.A);
						break;
					case 1:
						uvs.Add(UVCoord.B);
						break;
					case 2:
						uvs.Add(UVCoord.C);
						break;
				}
			}
			addedIndex++;
			addedIndex %= 3;
			adc = addedIndex == 0 ? !adc : adc;
		}
	}

	/*
   Linearly interpolate the position where an isosurface cuts
   an edge between two vertices, each with their own scalar value
*/
	Vector3 VertexInterp(double cellVisibleThreshold, Vector3 p1, Vector3 p2, double valp1, double valp2)
	{
		double mu;
		Vector3 p;

		if (Math.Abs(cellVisibleThreshold - valp1) < 0.00001)
			return (p1);
		if (Math.Abs(cellVisibleThreshold - valp2) < 0.00001)
			return (p2);
		if (Math.Abs(valp1 - valp2) < 0.00001)
			return (p1);
		mu = (cellVisibleThreshold - valp1) / (valp2 - valp1);
		p.x = p1.x + (float)mu * (p2.x - p1.x);
		p.y = p1.y + (float)mu * (p2.y - p1.y);
		p.z = p1.z + (float)mu * (p2.z - p1.z);

		return (p);
	}

	Vector3 InterpolateVerts(double cellVisibleThreshold, Vector3 p1, Vector3 p2, double valp1, double valp2)
	{
		float t = (float)((cellVisibleThreshold - valp1) / (valp2 - valp1));
		return p1 + t * (p2 - p1);
	}

	double GetNoiseValue(Vector3 pos, Vector3 offset)
	{
		double AB = Mathf.PerlinNoise(offset.x + gridNoiseScale * pos.x, offset.y + gridNoiseScale * pos.y);         // get all three(3) permutations of noise for x,y and z
		double BC = Mathf.PerlinNoise(offset.y + gridNoiseScale * pos.y, offset.z + gridNoiseScale * pos.z);
		double AC = Mathf.PerlinNoise(offset.x + gridNoiseScale * pos.x, offset.z + gridNoiseScale * pos.z);

		double BA = Mathf.PerlinNoise(offset.y + gridNoiseScale * pos.y, offset.x + gridNoiseScale * pos.x);         // and their reverses
		double CB = Mathf.PerlinNoise(offset.z + gridNoiseScale * pos.z, offset.y + gridNoiseScale * pos.y);
		double CA = Mathf.PerlinNoise(offset.z + gridNoiseScale * pos.z, offset.x + gridNoiseScale * pos.x);

		double ABC = (AB + BC + AC + BA + CB + CA) / 6.0;    // and return the average
		return ABC;
	}

	double GetNoiseValue2D(Vector3 pos, Vector3 offset)
	{
		return Mathf.PerlinNoise(offset.x + gridNoiseScale * pos.x, offset.z + gridNoiseScale * pos.z) * offset.y + gridNoiseScale * pos.y;
	}

	public Mesh GetMesh(ref GameObject go, ref Material material)
	{
		Mesh mesh = null;

		MeshFilter mf = go.GetComponent<MeshFilter>(); //add meshfilter component
		if (mf == null)
		{
			mf = go.AddComponent<MeshFilter>();
		}

		MeshRenderer mr = go.GetComponent<MeshRenderer>(); //add meshrenderer component
		if (mr == null)
		{
			mr = go.AddComponent<MeshRenderer>();
		}

		mr.material = material;

		mesh = mf.mesh;
		if (mesh == null)
		{
			mf.mesh = new Mesh();
			mesh = mf.mesh;
		}
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; //increase max vertices per mesh
		mesh.name = "ProceduralMesh";

		return mesh;
	}

	public void SetMesh(ref Transform transform, ref Mesh mesh)
	{
		//our mesh data to meshfilter
		mesh.Clear();

		for (int i = 0; i < vertices.Count; i++)
		{
			vertices[i] = transform.TransformPoint(vertices[i]);
		}

		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
	}

	public List<GridCell> GetGrid()
	{
		return grid;
	}

	public void SetNoiseScale(float value) => gridNoiseScale = value;
	public void SetNoiseOffset(Vector3 value) => noiseOffset = value;
	public void SetGridRadius(float value) => gridCellRadius = value;
	public void SetCellVisibleThreshold(float value) => cellVisibleThreshold = value;
	public void SetInverseTriangles(bool value) => inverseTriangles = value;
	public void SetGridMin(Vector3 value) => gridMin = value;
	public void SetGridMax(Vector3 value) => gridMax = value;
	public void SetMarch2D(bool value) => march2D = value;
}
