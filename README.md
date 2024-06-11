# RCache
This started as an alternative for a slower, and less efficient custom cache implementation used at work. It outperforms MemoryCache, so I decided to keep investigating and see if I could match JitBit's FastCache, which is incredibly fast. The table below reflects the current benchmark results.

# Current Results
| Method                             | CacheSize | Mean         | Error         | StdDev       | Gen0    | Allocated |
|----------------------------------- |---------- |-------------:|--------------:|-------------:|--------:|----------:|
| RichardCache_GetOrAdd              | 1000      | 95,005.80 ns | 115,075.18 ns | 6,307.659 ns | 16.4795 |  103944 B |
| RichardCache_LookupExistingKeys    | 1000      | 92,818.60 ns |  67,009.24 ns | 3,673.002 ns | 16.4795 |  103944 B |
| RichardCache_LookupNonExistingKeys | 1000      |     71.77 ns |      21.04 ns |     1.153 ns |  0.0166 |     104 B |
| FastCache_GetOrAdd                 | 1000      | 90,110.04 ns |  93,650.58 ns | 5,133.304 ns |  6.3477 |   39920 B |
| FastCache_LookupExistingKeys       | 1000      | 91,181.75 ns | 146,324.09 ns | 8,020.517 ns |  6.3477 |   39920 B |
| FastCache_LookupNonExistingKeys    | 1000      |     63.43 ns |      55.59 ns |     3.047 ns |  0.0063 |      40 B |



# Contributions
Please feel free to contribute.


