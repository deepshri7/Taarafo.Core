﻿// ---------------------------------------------------------------
// Copyright (c) Coalition of the Good-Hearted Engineers
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Taarafo.Core.Models.Groups;

namespace Taarafo.Core.Brokers.Storages
{
    public partial class StorageBroker
    {
        public DbSet<Group> Groups { get; set; }
        public async ValueTask<Group> InsertProfileAsync(Group group)
        {
            using var broker = new StorageBroker(this.configuration);

            EntityEntry<Group> groupEntityEntry =
                await broker.Groups.AddAsync(group);

            await broker.SaveChangesAsync();

            return groupEntityEntry.Entity;
        }
    }
}
