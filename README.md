This is a MIT-X11 implementation which tries to implement the interfaces from the nuget package:

https://nuget.org/packages/Microsoft.Bcl.Immutable

Unfortunately Microsoft didn't release their implementation under a license useable in mono I needed to reimplement the interfaces.
One difference is the implementation of the immutable list. I chose that the element access should be an O(1) operation. Unfortunately
that makes the insert/remove operations O(n) in the worst case. (However Add is approx O(1))
