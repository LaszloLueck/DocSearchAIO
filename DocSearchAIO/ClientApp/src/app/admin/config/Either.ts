export type Left<T> = {
  left: T;
  right?: never;
};

export type Right<U> = {
  left?: never;
  right: U;
};

export type Either<T, U> = NonNullable<Left<T> | Right<U>>;

export const isLeft = <T, U>(e: Either<T, U>): e is Left<T> => {
  return e.left !== undefined;
};

export const isRight = <T, U>(e: Either<T, U>): e is Right<U> => {
  return e.right !== undefined;
};

export function match<T, U>(either: Either<T, U>, left: (value: T) => void, right: (value: U) => void) {
  if (isLeft(either))
    left(either.left);
  if (isRight(either))
    right(either.right);
}


export function matchExec<T,U,V>(either: Either<T,U>, left: (value: T) => V, right: (value: U) => V){
  if(isLeft(either))
    return left(either.left);
  if(isRight(either))
    return right(either.right);

  throw new Error('Neither left or right is executable!');
}


export const makeLeft = <T>(value: T): Left<T> => ({left: value});

export const makeRight = <U>(value: U): Right<U> => ({right: value});

export type UnwrapEither = <T, U>(e: Either<T, U>) => NonNullable<T | U>;

export const unwrapEither: UnwrapEither = <T, U>({left, right}: Either<T, U>) => {
  if (right !== undefined && left !== undefined) {
    throw new Error(
      `Received both left and right values at runtime when opening an Either\nLeft: ${JSON.stringify(
        left
      )}\nRight: ${JSON.stringify(right)}`
    );
    /*
     We're throwing in this function because this can only occur at runtime if something
     happens that the TypeScript compiler couldn't anticipate. That means the application
     is in an unexpected state and we should terminate immediately.
    */
  }
  if (left !== undefined) {
    return left as NonNullable<T>; // Typescript is getting confused and returning this type as `T | undefined` unless we add the type assertion
  }
  if (right !== undefined) {
    return right as NonNullable<U>;
  }
  throw new Error(
    `Received no left or right values at runtime when opening Either`
  );
};
