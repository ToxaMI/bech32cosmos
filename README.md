bech32cosmos
==========

[![ISC License](http://img.shields.io/badge/license-ISC-blue.svg)](http://copyfree.org)

Package bech32cosmos provides a Go/Rust/Js implementation of the bech32cosmos format 

This form is derived from the format specified in specified in
[BIP 173](https://github.com/bitcoin/bips/blob/master/bip-0173.mediawiki).

The Go part of project is forked from https://github.com/bitcoin/bech32

bech32cosmos differs from the BIP 173 in two main respect. Mixed capitalization is permitted though the string is evaluated as lowercase. The seperator character between the human readable and data parts of a bech string is `:` instead of `1`.


## Installation and Updating

```bash
$ go get -u github.com/cosmos/bech32cosmos/go
```

Bech32Cosmos is Apache 2 licensed.
