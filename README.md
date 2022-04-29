# MarchingCubesAlgorithm
A static-class implementation of the MarchingCubes algorithm for creating a procedural mesh, based on a mix of three different approaches by:
* Sebastian Lague: https://www.youtube.com/watch?v=M3iI2l0ltbE
* Omar Santiago: https://www.youtube.com/watch?v=JdeyNbDACV0
* Paul Bourke: http://paulbourke.net/geometry/polygonise/

Visual example of the algorithm in action:

![MarchingCubes visual](/images/MarchingCubes_Showcase.gif)

The static helper-method for creating the grid that is needed for the mesh to be created:
![Creating marching grid](/images/MarchingCubes_Grid_Creation.png)
Parameters are as follows:
* The minimum edge-position of the grid-volume
* The maximum edge-position of the grid-volume
* The 3D position offset for the noise method
* The radius of a cell in the 3D grid, lower radius gives higher resolution but is slower
* The scale for the noise method
* The visibility threshold for a given edge of a cube in the grid
* An optional possibility to specify a pre-made mesh-configuration, right now there's only support for either perlin noise or a spherical appearance
* An optional configuration radius, specific for the cube and sphere mesh-configuration options

The static helper-method for creating the resulting mesh after creating the grid. Spits out the resulting mesh and returns whether a mesh could be created or not, returns the new mesh if possible, otherwise returns null:
![Creating marching mesh](/images/MarchingCubes_Getting_Mesh.png)
Parameters are as follows:
* The grid of cubes and their edge-positions and visibility values, stored in a "GridCell"-class list for ease of access
* The resulting mesh as an OUT parameter for retreiving the resulting mesh
* An optional string for the name of the resulting mesh
