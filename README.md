# BozoCrack (C# Port)
This is a C# port of [BozoCrack](https://github.com/juuso/BozoCrack)

BozoCrack is a depressingly effective MD5 password hash cracker with almost zero CPU/GPU load. Instead of rainbow tables, dictionaries, or brute force, BozoCrack simply *finds* the plaintext password. Specifically, it googles the MD5 hash and hopes the plaintext appears somewhere on the first page of results. It finds a list of words by splitting the results by spaces, slashes, colons, and HTML tags. It then tests each of these words to see if the resulting MD5 hash is equal to the input.

Just as the origininal Ruby version, it works way better than it should, and shows how something so simple can find your passwords.

![Screenshot](https://i.imgur.com/cbezDCq.png)

## Differences
My version, besides being in C#, supports a few differences:

- Ability to launch from command line and stand-alone .exe

- Ability to save results in `results.txt`

- Ability to edit search websites (`www.md5-hash.com` by default, google is also effective)

- Use of multiple search websites (In case the first does not return results)

- Constant loop, you may type 'exit' to exit the program, otherwise you can run it multiple times without reopening

- Probably a few other differences, just try it out :)

## Files
Quite a lot more than the original, due to the VS solution structure. If you would like to modify this program, the entirety of the code is contained within `src/BozoCrack/Program.cs`

The program will generate `search.txt` and `results.txt` in it's directory, one for a list of websites to use to look for results, and the other to store the results in. (Output is generated in console as well)

## How?
Command Line Usage:

    > BozoCrack.exe fcf1eed8596699624167416a1e7e122e
    > BozoCrack.exe C:\MD5_list.txt

Or, run the exe and follow the prompts.

The input file has no specified format. BozoCrack automatically picks up strings that look like MD5 hashes. However, a single line cannot contain more than 1 hash, but it can be surrounded by other strings.

File Example with output:

	>BozoCrack.exe C:\MD5_list.txt

	5 unique hashes found.

	fcf1eed8596699624167416a1e7e122e:octopus
	bed128365216c019988915ed3add75fb:passw0rd
	d0763edaa9d9bd2a9516280e9044d885:monkey
	dfd8c10c1b9b58c8bf102225ae3be9eb:12081977
	ede6b50e7b5826fe48fc1f0fe772c48f:1q2w3e4r5t6y
	
Hash Example with output:

	>BozoCrack.exe bed128365216c019988915ed3add75fb

	bed128365216c019988915ed3add75fb:passw0rd

## Why?
The original version by [juuso](https://github.com/juuso) and others was created to show just how bad an idea it is to use plain MD5 as a password hashing mechanism. Honestly, if the passwords can be cracked with *this software*, there are no excuses.

I created this as a coding exercise, to try and port it to C#.


## Who?
The original Ruby version of BozoCrack was written by [Juuso Salonen](http://twitter.com/juusosalonen), the guy behind [Radio Silence](http://radiosilenceapp.com) and [Private Eye](http://radiosilenceapp.com/private-eye).

This C# version was written by [Cyral](http://twitter.com/Cyral33).


## License
Just as the original, this is released in the Public Domain, do whatever you want with it! (Although linking back to this or the original would be appreciated)
