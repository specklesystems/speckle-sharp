This folder will be the new location for classes that represent the GSA schema.  The idea behind this is to separate:
- the textual parsing of GWA commands into meaningful typed values
- the logic of converting these typed values to/from Speckle-typed objects

These classes are essentially a dictionary representation of the GWA values and should only deviate from this concept for these situations:
1. A C# type can't represent the full range of values of a GWA parameter without using dynamic.  
For example: in LOAD_NODE commands, the same GWA parameter can store the string "GLOBAL" (essentially marking it as using the global 
axis/coordinates and not referencing any particular entry in the AXIS table) or an integer index of a record in the axis table.  
In this case, two class members (a bool and a nullable integer) are mapped to this one GWA parameter.

Other notes:
- nullable values are used for cases where GSA permits invalid or meaningless values to be stored.  A main example of this is a reference (index): a property index of zero is
  permitted but, since the lowest record value of any table is 1, means that it is an invalid value, and will be stored in the schema as null.  
  Another example is temperature or offsets - these aren't technically invalid, but is meaningless for real situations, so they'll be stored as null in the schema, too.