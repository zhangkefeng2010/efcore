// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.EntityFrameworkCore
{
    public class DatabaseFacadeTest
    {
        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Methods_delegate_to_configured_store_creator(bool async)
        {
            var creator = new FakeDatabaseCreator();

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDatabaseCreator>(creator));

            if (async)
            {
                Assert.True(await context.Database.EnsureCreatedAsync());
                Assert.Equal(1, creator.EnsureCreatedAsyncCount);

                Assert.True(await context.Database.EnsureDeletedAsync());
                Assert.Equal(1, creator.EnsureDeletedAsyncCount);

                Assert.True(await context.Database.CanConnectAsync());
                Assert.Equal(1, creator.CanConnectAsyncCount);
            }
            else
            {
                Assert.True(context.Database.EnsureCreated());
                Assert.Equal(1, creator.EnsureCreatedCount);

                Assert.True(context.Database.EnsureDeleted());
                Assert.Equal(1, creator.EnsureDeletedCount);

                Assert.True(context.Database.CanConnect());
                Assert.Equal(1, creator.CanConnectCount);
            }
        }

        private class FakeDatabaseCreator : IDatabaseCreator
        {
            public int CanConnectCount;
            public int CanConnectAsyncCount;
            public int EnsureDeletedCount;
            public int EnsureDeletedAsyncCount;
            public int EnsureCreatedCount;
            public int EnsureCreatedAsyncCount;

            public bool EnsureDeleted()
            {
                EnsureDeletedCount++;
                return true;
            }

            public Task<bool> EnsureDeletedAsync(CancellationToken cancellationToken = default)
            {
                EnsureDeletedAsyncCount++;
                return Task.FromResult(true);
            }

            public bool EnsureCreated()
            {
                EnsureCreatedCount++;
                return true;
            }

            public Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default)
            {
                EnsureCreatedAsyncCount++;
                return Task.FromResult(true);
            }

            public bool CanConnect()
            {
                CanConnectCount++;
                return true;
            }

            public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
            {
                CanConnectAsyncCount++;
                return Task.FromResult(true);
            }
        }

        [ConditionalFact]
        public void Can_get_IServiceProvider()
        {
            using var context = InMemoryTestHelpers.Instance.CreateContext();
            Assert.Same(
                ((IInfrastructure<IServiceProvider>)context).Instance,
                ((IInfrastructure<IServiceProvider>)context.Database).Instance);
        }

        [ConditionalFact]
        public void Can_get_DatabaseCreator()
        {
            using var context = InMemoryTestHelpers.Instance.CreateContext();
            Assert.Same(
                context.GetService<IDatabaseCreator>(),
                context.Database.GetService<IDatabaseCreator>());
        }

        [ConditionalFact]
        public void Can_get_Model()
        {
            using var context = InMemoryTestHelpers.Instance.CreateContext();
            Assert.Same(context.GetService<IModel>(), context.Database.GetService<IModel>());
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Can_begin_transaction(bool async)
        {
            var transaction = new FakeDbContextTransaction();

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDbContextTransactionManager>(
                    new FakeDbContextTransactionManager(transaction)));

            Assert.Same(
                transaction,
                async
                    ? await context.Database.BeginTransactionAsync()
                    : context.Database.BeginTransaction());
        }

        private class FakeDbContextTransactionManager : IDbContextTransactionManager
        {
            private readonly FakeDbContextTransaction _transaction;

            public FakeDbContextTransactionManager(FakeDbContextTransaction transaction)
            {
                _transaction = transaction;
            }

            public int CommitCalls;
            public int RollbackCalls;
            public int CreateSavepointCalls;
            public int RollbackSavepointCalls;
            public int ReleaseSavepointCalls;
            public int AreSavepointsSupportedCalls;

            public IDbContextTransaction BeginTransaction()
                => _transaction;

            public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IDbContextTransaction>(_transaction);

            public void CommitTransaction() => CommitCalls++;

            public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
            {
                CommitCalls++;
                return Task.CompletedTask;
            }

            public void RollbackTransaction() => RollbackCalls++;

            public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
            {
                RollbackCalls++;
                return Task.CompletedTask;
            }

            public void CreateSavepoint(string savepointName) => CreateSavepointCalls++;

            public Task CreateSavepointAsync(string savepointName, CancellationToken cancellationToken = default)
            {
                CreateSavepointCalls++;
                return Task.CompletedTask;
            }

            public void RollbackSavepoint(string savepointName) => RollbackSavepointCalls++;

            public Task RollbackSavepointAsync(string savepointName, CancellationToken cancellationToken = default)
            {
                RollbackSavepointCalls++;
                return Task.CompletedTask;
            }

            public void ReleaseSavepoint(string savepointName) => ReleaseSavepointCalls++;

            public Task ReleaseSavepointAsync(string savepointName, CancellationToken cancellationToken = default)
            {
                ReleaseSavepointCalls++;
                return Task.CompletedTask;
            }

            public bool AreSavepointsSupported
            {
                get
                {
                    AreSavepointsSupportedCalls++;
                    return true;
                }
            }

            public IDbContextTransaction CurrentTransaction => _transaction;
            public Transaction EnlistedTransaction { get; }
            public void EnlistTransaction(Transaction transaction) => throw new NotImplementedException();

            public void ResetState() => throw new NotImplementedException();
            public Task ResetStateAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        private class FakeDbContextTransaction : IDbContextTransaction
        {
            public void Dispose() => throw new NotImplementedException();
            public ValueTask DisposeAsync() => throw new NotImplementedException();
            public Guid TransactionId { get; }
            public void Commit() => throw new NotImplementedException();
            public Task CommitAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public void Rollback() => throw new NotImplementedException();
            public Task RollbackAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public void Save(string savepointName) => throw new NotImplementedException();
            public Task SaveAsync(string savepointName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public void Rollback(string savepointName) => throw new NotImplementedException();
            public Task RollbackAsync(string savepointName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public void Release(string savepointName) => throw new NotImplementedException();
            public Task ReleaseAsync(string savepointName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public bool AreSavepointsSupported => throw new NotImplementedException();
        }

        [ConditionalFact]
        public void Can_commit_transaction()
        {
            var manager = new FakeDbContextTransactionManager(new FakeDbContextTransaction());

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDbContextTransactionManager>(manager));

            context.Database.CommitTransaction();

            Assert.Equal(1, manager.CommitCalls);
        }

        [ConditionalFact]
        public async Task Can_commit_transaction_async()
        {
            var manager = new FakeDbContextTransactionManager(new FakeDbContextTransaction());

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDbContextTransactionManager>(manager));

            await context.Database.CommitTransactionAsync();

            Assert.Equal(1, manager.CommitCalls);
        }

        [ConditionalFact]
        public void Can_roll_back_transaction()
        {
            var manager = new FakeDbContextTransactionManager(new FakeDbContextTransaction());

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDbContextTransactionManager>(manager));

            context.Database.RollbackTransaction();

            Assert.Equal(1, manager.RollbackCalls);
        }

        [ConditionalFact]
        public async Task Can_roll_back_transaction_async()
        {
            var manager = new FakeDbContextTransactionManager(new FakeDbContextTransaction());

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDbContextTransactionManager>(manager));

            await context.Database.RollbackTransactionAsync();

            Assert.Equal(1, manager.RollbackCalls);
        }

        [ConditionalFact]
        public void Can_create_savepoint()
        {
            var manager = new FakeDbContextTransactionManager(new FakeDbContextTransaction());

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDbContextTransactionManager>(manager));

            context.Database.CreateSavepoint("foo");

            Assert.Equal(1, manager.CreateSavepointCalls);
        }

        [ConditionalFact]
        public async Task Can_create_savepoint_async()
        {
            var manager = new FakeDbContextTransactionManager(new FakeDbContextTransaction());

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDbContextTransactionManager>(manager));

            await context.Database.CreateSavepointAsync("foo");

            Assert.Equal(1, manager.CreateSavepointCalls);
        }

        [ConditionalFact]
        public void Can_rollback_savepoint()
        {
            var manager = new FakeDbContextTransactionManager(new FakeDbContextTransaction());

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDbContextTransactionManager>(manager));

            context.Database.RollbackSavepoint("foo");

            Assert.Equal(1, manager.RollbackSavepointCalls);
        }

        [ConditionalFact]
        public async Task Can_rollback_savepoint_async()
        {
            var manager = new FakeDbContextTransactionManager(new FakeDbContextTransaction());

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDbContextTransactionManager>(manager));

            await context.Database.RollbackSavepointAsync("foo");

            Assert.Equal(1, manager.RollbackSavepointCalls);
        }

        [ConditionalFact]
        public void Can_release_savepoint()
        {
            var manager = new FakeDbContextTransactionManager(new FakeDbContextTransaction());

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDbContextTransactionManager>(manager));

            context.Database.ReleaseSavepoint("foo");

            Assert.Equal(1, manager.ReleaseSavepointCalls);
        }

        [ConditionalFact]
        public async Task Can_release_savepoint_async()
        {
            var manager = new FakeDbContextTransactionManager(new FakeDbContextTransaction());

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDbContextTransactionManager>(manager));

            await context.Database.ReleaseSavepointAsync("foo");

            Assert.Equal(1, manager.ReleaseSavepointCalls);
        }

        [ConditionalFact]
        public void Can_check_if_checkpoints_are_supported()
        {
            var manager = new FakeDbContextTransactionManager(new FakeDbContextTransaction());

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDbContextTransactionManager>(manager));

            _ = context.Database.AreSavepointsSupported;

            Assert.Equal(1, manager.AreSavepointsSupportedCalls);
        }

        [ConditionalFact]
        public void Can_get_current_transaction()
        {
            var transaction = new FakeDbContextTransaction();

            var context = InMemoryTestHelpers.Instance.CreateContext(
                new ServiceCollection().AddSingleton<IDbContextTransactionManager>(
                    new FakeDbContextTransactionManager(transaction)));

            Assert.Same(transaction, context.Database.CurrentTransaction);
        }

        [ConditionalFact]
        public void Cannot_use_DatabaseFacade_after_dispose()
        {
            var context = InMemoryTestHelpers.Instance.CreateContext();
            var facade = context.Database;
            context.Dispose();

            Assert.Throws<ObjectDisposedException>(() => context.Database.GetService<IModel>());

            foreach (var methodInfo in facade.GetType().GetMethods(BindingFlags.Public))
            {
                Assert.Throws<ObjectDisposedException>(() => methodInfo.Invoke(facade, null));
            }
        }
    }
}
