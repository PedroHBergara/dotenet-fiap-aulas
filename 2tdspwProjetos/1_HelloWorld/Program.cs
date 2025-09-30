// See https://aka.ms/new-console-template for more information

Console.WriteLine("Hello, World!");
var texto = Console.ReadLine();
Console.WriteLine($"Texto digitado: {texto}");

// variaveis
var numeroInteiro = 20;
var numeroDecimal = 20.5;
var numeroFlutuante = 20.5f;
var numeroDecimalGrande = 20.5m;

Console.WriteLine("Digite seu número:");
float numero = float.Parse(Console.ReadLine());

var caracter = 'L';
var textoPequeno = "Olá, Mundo!";
var textoGrande = """
                  Olá mundo!
                  Como você está?;
                  tudo bem e você?
                  """;

Console.WriteLine(textoGrande);

//boolean
var verdadeiro = true;

//Operadores de decisão
if (numero > 10)
    Console.WriteLine("O número é maior que 10");
else
    Console.WriteLine("O número é menor que 10");

//por operador ternário
Console.WriteLine(numero > 10 ? 
    "O número é maior que 10" : 
    "O número é menor que 10");

//switch case
switch (numero)
{
    case 10:
        Console.WriteLine("O número é 10");
        break;
    case 20:
        Console.WriteLine("O número é 20");
        break;
    default:
        Console.WriteLine("O número é diferente de 10 e 20");
        break;
}

//Laços de repetição
var i = 0;
while (i <= 10)
{
    Console.WriteLine(i);
    i++;
}

var numeros = new int[]{11,12,13,14,15,16,17,18,19,20};
foreach (var n in numeros)
    Console.WriteLine(n);

var numerosPrimos = new List<int> { 2, 3, 5, 7, 11, 13, 17, 19 };


