/**
 * ESM shim for async-lock@1.4.1
 *
 * async-lock ships only as CJS (module.exports = AsyncLock).
 * Vite cannot handle CJS default-import inside the pnpm virtual store.
 * This file embeds the full source as ESM so Vite never touches the CJS file.
 *
 * Source: https://github.com/rogierschouten/async-lock (MIT)
 */

/* eslint-disable */
// @ts-nocheck

class AsyncLock {
	constructor(opts?: any) {
		opts = opts || {};
		this.Promise = opts.Promise || Promise;
		this.queues = Object.create(null);
		this.domainReentrant = opts.domainReentrant || false;
		this.timeout = opts.timeout || AsyncLock.DEFAULT_TIMEOUT;
		this.maxOccupationTime = opts.maxOccupationTime || AsyncLock.DEFAULT_MAX_OCCUPATION_TIME;
		this.maxExecutionTime = opts.maxExecutionTime || AsyncLock.DEFAULT_MAX_EXECUTION_TIME;
		if (
			opts.maxPending === Infinity ||
			(Number.isInteger(opts.maxPending) && opts.maxPending >= 0)
		) {
			this.maxPending = opts.maxPending;
		} else {
			this.maxPending = AsyncLock.DEFAULT_MAX_PENDING;
		}
	}

	static DEFAULT_TIMEOUT = 0;
	static DEFAULT_MAX_OCCUPATION_TIME = 0;
	static DEFAULT_MAX_EXECUTION_TIME = 0;
	static DEFAULT_MAX_PENDING = 1000;

	acquire(key: any, fn: any, cb?: any, opts?: any): any {
		if (Array.isArray(key)) {
			return this._acquireBatch(key, fn, cb, opts);
		}
		if (typeof fn !== 'function') {
			throw new Error('You must pass a function to execute');
		}

		let deferredResolve: any = null;
		let deferredReject: any = null;
		let deferred: any = null;

		if (typeof cb !== 'function') {
			opts = cb;
			cb = null;
			deferred = new this.Promise((resolve: any, reject: any) => {
				deferredResolve = resolve;
				deferredReject = reject;
			});
		}

		opts = opts || {};
		let resolved = false;
		let timer: any = null;
		let occupationTimer: any = null;
		let executionTimer: any = null;
		const self = this;

		const done = (locked: boolean, err?: any, ret?: any) => {
			if (occupationTimer) {
				clearTimeout(occupationTimer);
				occupationTimer = null;
			}
			if (executionTimer) {
				clearTimeout(executionTimer);
				executionTimer = null;
			}
			if (locked) {
				if (self.queues[key] && self.queues[key].length === 0) delete self.queues[key];
			}
			if (!resolved) {
				if (!deferred) {
					if (typeof cb === 'function') cb(err, ret);
				} else {
					if (err) deferredReject(err);
					else deferredResolve(ret);
				}
				resolved = true;
			}
			if (locked && self.queues[key] && self.queues[key].length > 0) {
				self.queues[key].shift()();
			}
		};

		const exec = (locked: boolean) => {
			if (resolved) return done(locked);
			if (timer) {
				clearTimeout(timer);
				timer = null;
			}

			const maxExecutionTime = opts.maxExecutionTime || self.maxExecutionTime;
			if (maxExecutionTime) {
				executionTimer = setTimeout(() => {
					if (self.queues[key])
						done(locked, new Error('Maximum execution time is exceeded ' + key));
				}, maxExecutionTime);
			}

			if (fn.length === 1) {
				let called = false;
				try {
					fn((err: any, ret: any) => {
						if (!called) {
							called = true;
							done(locked, err, ret);
						}
					});
				} catch (err) {
					if (!called) {
						called = true;
						done(locked, err);
					}
				}
			} else {
				self._promiseTry(() => fn()).then(
					(ret: any) => done(locked, undefined, ret),
					(err: any) => done(locked, err)
				);
			}
		};

		const maxPending = opts.maxPending !== undefined ? opts.maxPending : self.maxPending;

		if (!self.queues[key]) {
			self.queues[key] = [];
			exec(true);
		} else if (self.queues[key].length >= maxPending) {
			done(false, new Error('Too many pending tasks in queue ' + key));
		} else {
			const taskFn = () => exec(true);
			if (opts.skipQueue) self.queues[key].unshift(taskFn);
			else self.queues[key].push(taskFn);

			const timeout = opts.timeout || self.timeout;
			if (timeout) {
				timer = setTimeout(() => {
					timer = null;
					done(false, new Error('async-lock timed out in queue ' + key));
				}, timeout);
			}
		}

		const maxOccupationTime = opts.maxOccupationTime || self.maxOccupationTime;
		if (maxOccupationTime) {
			occupationTimer = setTimeout(() => {
				if (self.queues[key])
					done(false, new Error('Maximum occupation time is exceeded in queue ' + key));
			}, maxOccupationTime);
		}

		if (deferred) return deferred;
	}

	_acquireBatch(keys: any[], fn: any, cb?: any, opts?: any): any {
		if (typeof cb !== 'function') {
			opts = cb;
			cb = null;
		}
		const self = this;
		const getFn = (key: any, fn: any) => (cb: any) => self.acquire(key, fn, cb, opts);
		const fnx = keys.reduceRight((prev, key) => getFn(key, prev), fn);
		if (typeof cb === 'function') {
			fnx(cb);
		} else {
			return new this.Promise((resolve: any, reject: any) => {
				if (fnx.length === 1) {
					fnx((err: any, ret: any) => {
						if (err) reject(err);
						else resolve(ret);
					});
				} else {
					resolve(fnx());
				}
			});
		}
	}

	isBusy(key?: string): boolean {
		if (!key) return Object.keys(this.queues).length > 0;
		return !!this.queues[key];
	}

	_promiseTry(fn: () => any): Promise<any> {
		try {
			return this.Promise.resolve(fn());
		} catch (e) {
			return this.Promise.reject(e);
		}
	}
}

export default AsyncLock;
export { AsyncLock };
