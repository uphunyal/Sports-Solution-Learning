using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using SportsStore.Controllers;
using SportsStore.Infrastructure;
using SportsStore.Models;
using SportsStore.Models.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SportsStore.Tests
{
    public class ProductControllerTests
    {
        [Fact]
        public void Can_UseRepository()
        {
            //Arrange
            Mock<IStoreRepository> mock = new Mock<IStoreRepository>();
            mock.Setup(m => m.Products).Returns((new Product[]
            {
                new Product {ProductID=1, Name="P1"},
                new Product{ProductID=2, Name="P2"}
            }).AsQueryable<Product>());

            HomeController controller = new HomeController(mock.Object);

            //Act
            ProductsListViewModel result = (controller.Index(null) as ViewResult).ViewData.Model as ProductsListViewModel;

            //Assert 
            Product[] prodArray = result.Products.ToArray();
            Assert.True(prodArray.Length == 2);
            Assert.Equal("P1", prodArray[0].Name);
            Assert.Equal("P2", prodArray[1].Name);
        }

        [Fact]
        public void Can_Paginate()
        {
            //Arrange
            Mock<IStoreRepository> mock = new Mock<IStoreRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] {
                new Product {ProductID = 1, Name = "P1"},
                new Product {ProductID = 2, Name = "P2"},
                new Product {ProductID = 3, Name = "P3"},
                new Product {ProductID = 4, Name = "P4"},
                new Product {ProductID = 5, Name = "P5"}
            }).AsQueryable<Product>());

            HomeController controller = new HomeController(mock.Object);
            controller.PageSize = 3;


            //Act 
            ProductsListViewModel result = (controller.Index(null,2) as ViewResult).ViewData.Model as ProductsListViewModel;


            //Assert
            Product[] prodArray = result.Products.ToArray();
            Assert.True(prodArray.Length == 2);
            Assert.Equal("P4", prodArray[0].Name);

        }
        [Fact]
        public void Can_Generate_Page_Links()
        {
            // Arrange
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.SetupSequence(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns("Test/Page1")
            .Returns("Test/Page2")
            .Returns("Test/Page3");
           
            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory.Setup(f =>
            f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelper.Object);
            PageLinkTagHelper helper =
            new PageLinkTagHelper(urlHelperFactory.Object)
            {
                PageModel = new PagingInfo
                {
                    CurrentPage = 2,
                    TotalItems = 28,
                    ItemsPerPage = 10
                },
                PageAction = "Test"
            };
            TagHelperContext ctx = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(), "");
            var content = new Mock<TagHelperContent>();
            TagHelperOutput output = new TagHelperOutput("div",
            new TagHelperAttributeList(),
            (cache, encoder) => Task.FromResult(content.Object));
            // Act
            helper.Process(ctx, output);
            // Assert
            Assert.Equal(@"<a href=""Test/Page1"">1</a>"
            + @"<a href=""Test/Page2"">2</a>"
            + @"<a href=""Test/Page3"">3</a>",
            output.Content.GetContent());
        }

        [Fact]
        public void Can_Send_Pagination_View_Model()
        {
            //Arrange
            Mock<IStoreRepository> mock = new Mock<IStoreRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] {
                new Product {ProductID = 1, Name = "P1"},
                new Product {ProductID = 2, Name = "P2"},
                new Product {ProductID = 3, Name = "P3"},
                new Product {ProductID = 4, Name = "P4"},
                new Product {ProductID = 5, Name = "P5"}
            }).AsQueryable<Product>());

            //Arrange
            HomeController controller = new HomeController(mock.Object) { PageSize = 3 };

            //Act

            ProductsListViewModel result = controller.Index(null,2).ViewData.Model as ProductsListViewModel;

            //Assert 
            Product[] prodArray = result.Products.ToArray();
            Assert.True(prodArray.Length == 2);
            Assert.Equal("P4", prodArray[0].Name);
            Assert.Equal("P5", prodArray[1].Name);
        }

        [Fact]
        public void Can_Filter_Products()
        {
            //Arrange, create mock repository
            Mock<IStoreRepository> mock = new Mock<IStoreRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] {
                new Product {ProductID = 1, Name = "P1", Category = "Cat1"},
                new Product {ProductID = 2, Name = "P2", Category = "Cat2"},
                new Product {ProductID = 3, Name = "P3", Category = "Cat1"},
                new Product {ProductID = 4, Name = "P4", Category = "Cat2"},
                new Product {ProductID = 5, Name = "P5", Category = "Cat3"}
}).AsQueryable<Product>());

            //Arramge, create controller and set page size to 3
            HomeController controller = new HomeController(mock.Object);
            controller.PageSize=3;

            //Act
            Product[] result = (controller.Index("Cat2", 1).ViewData.Model as ProductsListViewModel).Products.ToArray();

            //Assert
            Assert.Equal(2, result.Length);
            Assert.True(result[0].Name == "P2" && result[0].Category == "Cat2");
            Assert.True(result[1].Name == "P4" && result[1].Category == "Cat2");


        }

        [Fact]
        public void Generate_Category_Specific_Product_Count()
        {
            //Arrange
            Mock<IStoreRepository> mock = new Mock<IStoreRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] {
                new Product {ProductID = 1, Name = "P1", Category = "Cat1"},
                new Product {ProductID = 2, Name = "P2", Category = "Cat2"},
                new Product {ProductID = 3, Name = "P3", Category = "Cat1"},
                new Product {ProductID = 4, Name = "P4", Category = "Cat2"},
                new Product {ProductID = 5, Name = "P5", Category = "Cat3"}
            }).AsQueryable<Product>());
            HomeController target = new HomeController(mock.Object);
            target.PageSize = 3;
            Func<ViewResult, ProductsListViewModel> GetModel = result => result?.ViewData?.Model as ProductsListViewModel;

            //Act
            int? res1 = GetModel(target.Index("Cat1")).PagingInfo.TotalItems;
            int? res2 = GetModel(target.Index("Cat2"))?.PagingInfo.TotalItems;
            int? res3 = GetModel(target.Index("Cat3"))?.PagingInfo.TotalItems;
            int? resAll = GetModel(target.Index(null))?.PagingInfo.TotalItems;

            //Assert 
            Assert.Equal(2, res1);
            Assert.Equal(2, res2);
            Assert.Equal(1, res3);
            Assert.Equal(5, resAll);


        }
    }
}
