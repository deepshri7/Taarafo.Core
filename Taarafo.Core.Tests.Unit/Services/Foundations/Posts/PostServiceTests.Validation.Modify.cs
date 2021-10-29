﻿using System;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Taarafo.Core.Models.Posts;
using Taarafo.Core.Models.Posts.Exceptions;
using Xunit;

namespace Taarafo.Core.Tests.Unit.Services.Foundations.Posts
{
    public partial class PostServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfPostIsNullAndLogItAsync()
        {
            // given
            Post nullPost = null;
            var nullPostException = new NullPostException();

            var expectedPostValidationException =
                new PostValidationException(nullPostException);

            // when
            ValueTask<Post> modifyPostTask =
                this.postService.ModifyPostAsync(nullPost);

            // then
            await Assert.ThrowsAsync<PostValidationException>(() =>
                modifyPostTask.AsTask());

            this.loggingBrokerMock.Verify(broker =>
                broker.LogError(It.Is(
                    SameValidationExceptionAs(expectedPostValidationException))),
                        Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdatePostAsync(It.IsAny<Post>()),
                    Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnModifyIfPostIsInvalidAndLogItAsync(string invalidText)
        {
            // given 
            var invalidPost = new Post
            {
                Content = invalidText
            };

            var invalidPostException = new InvalidPostException();

            invalidPostException.AddData(
                key: nameof(Post.Id),
                values: "Id is required");

            invalidPostException.AddData(
                key: nameof(Post.Content),
                values: "Text is required");

            invalidPostException.AddData(
                key:nameof(Post.Author),
                values:"Id is required");

            invalidPostException.AddData(
                key: nameof(Post.CreatedDate),
                values: "Date is required");

            invalidPostException.AddData(
                key: nameof(Post.UpdatedDate),
                values: "Date is required");

            var expectedPostValidationException =
                new PostValidationException(invalidPostException);

            // when
            ValueTask<Post> modifyPostTask =
                this.postService.ModifyPostAsync(invalidPost);

            //then
            await Assert.ThrowsAsync<PostValidationException>(() =>
                modifyPostTask.AsTask());

            this.loggingBrokerMock.Verify(broker =>
                broker.LogError(It.Is(SameValidationExceptionAs(
                    expectedPostValidationException))),
                        Times.Once());

            this.storageBrokerMock.Verify(broker =>
                broker.UpdatePostAsync(It.IsAny<Post>()),
                    Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedDateIsSameToCreatedDateAndLogItAsync()
        {
            // given
            DateTimeOffset dateTime = GetRadnomDateTimeOffset();
            Post randomPost = CreateRandomPost(dateTime);
            Post invalidPost = randomPost;
            var invalidPostException =
                new InvalidPostException();

            invalidPostException.AddData(
                key: nameof(Post.UpdatedDate),
                values: $"UpdatedDate is same as {nameof(Post.CreatedDate)}");

            var expectedPostValidationException = 
                new PostValidationException(invalidPostException);
            
            // when
            ValueTask<Post> modifyPostTask = 
                this.postService.ModifyPostAsync(invalidPost);

            // then
            await Assert.ThrowsAsync<PostValidationException>(() =>
                modifyPostTask.AsTask());

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffset(),
                    Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogError(It.Is(SameExceptionAs(
                    expectedPostValidationException))),
                        Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectPostByIdAsync(invalidPost.Id),
                    Times.Never);
            
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
        }
        
        [Theory]
        [MemberData(nameof(InvalidMinuteCases))]
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedDateIsNotRecentAndLogItAsync(int minutes)
        {
            // given
            DateTimeOffset dateTime = GetRadnomDateTimeOffset();
            Post randomPost = CreateRandomPost(dateTime);
            Post inputPost = randomPost;
            inputPost.UpdatedDate = dateTime.AddMinutes(minutes);
            var invalidPostException = 
                new InvalidPostException();

            invalidPostException.AddData(
                key:nameof(Post.UpdatedDate),
                values:"Date is not recent");

            var expectedPostValidatonException =
                new PostValidationException(invalidPostException);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffset())
                .Returns(dateTime);

            // when
            ValueTask<Post> modifyPostTask =
                this.postService.ModifyPostAsync(inputPost);

            // then
            await Assert.ThrowsAsync<PostValidationException>(() =>
                modifyPostTask.AsTask());

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffset(), 
                    Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogError(It.Is(SameExceptionAs(
                    expectedPostValidatonException))),
                        Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectPostByIdAsync(It.IsAny<Guid>()),
                    Times.Never);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfPostDoesNotExistAndLogItAsync()
        {
            // given
            int randomNegativeMinutes = GetRandomNegativeNumber();
            DateTimeOffset dateTime = GetRandomDateTimeOffset();
            Post randomPost = CreateRandomPost(dateTime);
            Post nonExistPost = randomPost;
            nonExistPost.CreatedDate = dateTime.AddMinutes(randomNegativeMinutes);
            Post nullPost = null;

            var notFoundPostException =
                new NotFoundPostException(nonExistPost.Id);

            var expectedPostValidationException =
                new PostValidationException(notFoundPostException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectPostByIdAsync(nonExistPost.Id))
                .ReturnsAsync(nullPost);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffset())
                .Returns(dateTime);

            // when 
            ValueTask<Post> modifyPostTask =
                this.postService.ModifyPostAsync(nonExistPost);

            // then
            await Assert.ThrowsAsync<PostValidationException>(() =>
                modifyPostTask.AsTask());

            this.storageBrokerMock.Verify(broker =>
                broker.SelectPostByIdAsync(It.IsAny<Guid>()), 
                    Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffset(),
                    Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogError(It.Is(SameExceptionAs(
                    expectedPostValidationException))),
                        Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}