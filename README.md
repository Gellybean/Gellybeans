# Gellybeans

Gellybeans is a C# library containing an expression parser built primarily for [Mathfinder](https://github.com/Gellybean/MathfinderBot), a Discord bot. Check out the [MF wiki](https://github.com/Gellybean/MathfinderBot/wiki) for more information.

Gellybeans supports:
- Integer math, interpolated strings, bools, arrays
- Variable assignment
- Dice expressions
- Conditionals
- For loops
- Functions

```
fizzbuzz = -> (count)
{
	
	** i : 0..count : {
		i % 3 == 0 && i % 5 == 0 ?? {
			print("FizzBuzz");
		} : i % 3 == 0 {
			print("Fizz)";
		} : i % 5 == 0 {
			print("Buzz");
		} : {
			print(i);
		}				
	}
}

```

``` fizzbuzz(100) ```


Output (From Mathfinder)

![Screenshot 2024-05-10 193557](https://github.com/Gellybean/Gellybeans/assets/10622391/f7985bdd-d83a-48f0-a578-8df5e82e2d2e)
