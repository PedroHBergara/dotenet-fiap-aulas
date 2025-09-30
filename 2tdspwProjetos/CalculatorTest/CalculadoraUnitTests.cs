using System.Runtime.InteropServices;

namespace CalculatorTest;

public class CalculadoraUnitTests
{
    [Fact]
    public void Somar_DoisNumerosDeveRetornarSomaCorreta()
    {
        // Arrange
        var calculadora = new Calculadora();
        var a = 5;
        var b = 3;

        // Act
        var resultado = calculadora.Somar(a, b);
        
        // Assert
        Assert.Equal(8, resultado);
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(1, -2, -1)]
    [InlineData(0, 0, 0)]
    [InlineData(-1, -1, -2)]
    public void Somar_VariosNumerosDeveRetornarSomaCorreta(int a, int b, int esperado)
    {
        //Arrange
        var calculadora = new Calculadora();

        // Act
        var resultado = calculadora.Somar(a, b);
        
        // Assert
        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void Dividir_DivisaoPorZeroDeveLancarExcecao()
    {
        // Arrange
        var calculadora = new Calculadora();

        var a = 10;
        var b = 0;
        
        //Act & Assert
        Assert.Throws<DivideByZeroException>(() => calculadora.Dividir(10, 0));
    }
}