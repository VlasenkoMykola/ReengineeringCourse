using NetArchTest.Rules;
using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        [Test]
        public void Messages_Should_Not_Depend_On_Networking()
        {
            var result = Types.InAssembly(typeof(NetSdrMessageHelper).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Messages")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Networking")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "Messages layer must not depend on Networking layer. " +
                $"Violating types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
        }

        [Test]
        public void Networking_Should_Not_Depend_On_Messages()
        {
            var result = Types.InAssembly(typeof(NetSdrMessageHelper).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Networking")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Messages")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "Networking layer must not depend on Messages layer. " +
                $"Violating types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
        }

        [Test]
        public void Networking_Interfaces_Should_Not_Have_Dependency_On_System_Net_Sockets()
        {
            var result = Types.InAssembly(typeof(NetSdrMessageHelper).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Networking")
                .And()
                .AreInterfaces()
                .ShouldNot()
                .HaveDependencyOn("System.Net.Sockets")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "Networking interfaces must not depend on System.Net.Sockets directly. " +
                $"Violating types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
        }

        [Test]
        public void MessageHelper_Should_Be_Static()
        {
            var result = Types.InAssembly(typeof(NetSdrMessageHelper).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Messages")
                .And()
                .HaveNameEndingWith("Helper")
                .Should()
                .BeAbstract()  // static classes are compiled as abstract sealed
                .And()
                .BeSealed()
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "Helper classes in Messages should be static. " +
                $"Violating types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
        }
    }
}
