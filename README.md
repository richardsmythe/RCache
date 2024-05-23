# RCache
This started as an alternative for a slower, and less efficient custom cache implementation used at work. It outperforms MemoryCache, so I decided to keep investigating and see if I could match JitBit's FastCache which is incredibly fast.  

Job=ShortRun  IterationCount=3  LaunchCount=1 WarmupCount=3

# Current Results
| Method                             | CacheSize | Mean (ns)    |
|----------------------------------- |---------- |-------------:|
| RCache_GetOrAdd                    | 1000      |112,681.59    |
| MemoryCache_Add                    | 1000      | 305,383.25    |
| FastCache_GetOrAdd                 | 1000      |   67,608.04   |

# Contributions
Please feel free to contribute.


