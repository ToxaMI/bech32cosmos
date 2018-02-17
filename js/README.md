# bech32cosmos

A Bech32Cosmos encoding/decoding library.


## Example
``` javascript
let bech32cosmos = require('bech32cosmos')

bech32cosmos.decode('abcdef:qpzry9x8gf2tvdw0s3jn54khce6mua7lmqqqxw')
// => {
// 	 prefix: 'abcdef',
// 	 words: [0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31]
// }

let words = bech32cosmos.toWords(Buffer.from('foobar', 'utf8'))
bech32cosmos.encode('foo', words)
// => 'foo1vehk7cnpwgry9h96'
```





## License APACHE2