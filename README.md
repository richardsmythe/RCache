# RCache
This started as an alternative for a slower, and less efficient custom cache implementation used at work. I decided to keep investigating and see if I could match JitBit's FastCache. 

# Current Results
| Method                             | CacheSize | Mean (ns)    |
|----------------------------------- |---------- |-------------:|
| RichardCache_GetOrAdd              | 1000      | 169,886.9    |
| MemoryCache_Add                    | 1000      | 330,461.7    |
| FastCache_GetOrAdd                 | 1000      |  77,088.9    |

# Contributions
Please feel free to contribute.


