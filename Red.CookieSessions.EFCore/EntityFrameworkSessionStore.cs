﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Red.CookieSessions.EFCore
{
    public class EntityFrameworkSessionStore<TSession> : ICookieStore<TSession>
        where TSession : class, ICookieSession, new()
    {
        private readonly Func<DbContext> _getContext;
        private readonly bool _asNoTracking;

        public EntityFrameworkSessionStore(Func<DbContext> getContextContext, bool asNoTracking = false)
        {
            _getContext = getContextContext;
            _asNoTracking = asNoTracking;
        }


        public async Task<TSession?> TryGet(string sessionId)
        {
            await using var db = _getContext();
            var queryable = _asNoTracking ? db.Set<TSession>().AsNoTracking() : db.Set<TSession>();
            return await queryable.FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        public async Task<bool> TryRemove(string sessionId)
        {
            await using var db = _getContext();
            var queryable = _asNoTracking ? db.Set<TSession>().AsNoTracking() : db.Set<TSession>();
            var result = await queryable.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (result == default)
                return false;
            
            db.Set<TSession>().Remove(result);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task Set(TSession session)
        {
            await using var db = _getContext();
            var queryable = _asNoTracking ? db.Set<TSession>().AsNoTracking() : db.Set<TSession>();
            var result = await queryable.FirstOrDefaultAsync(s => s.Id == session.Id);
            if (result != default)
            {
                db.Remove(result);
            }
            db.Add(session);
            await db.SaveChangesAsync();
        }

        public async Task RemoveExpired()
        {
            await using var db = _getContext();
            var now = DateTime.UtcNow;
            var expired = db.Set<TSession>().Where(s => s.Expiration <= now);
            db.RemoveRange(expired);
            await db.SaveChangesAsync();
        }
    }
}