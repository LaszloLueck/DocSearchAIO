import {SuggestOrderPipe} from "./SuggestOrder.pipe";
import {SuggestResponseDetail} from "../interfaces/SuggestResponseDetail";

describe('SuggestOrderPipe', () => {
  const pipe = new SuggestOrderPipe();

  const a: SuggestResponseDetail = {
    label: 'abc',
    indexNames: ['a', 'b', 'c']
  };

  const b: SuggestResponseDetail = {
    label: 'bcd',
    indexNames: ['a', 'b', 'c']
  };

  const c: SuggestResponseDetail = {
    label: 'cde',
    indexNames: ['a', 'b', 'c']
  };

  const suggests: SuggestResponseDetail[] = [a,b,c];


  it('generates', () => {
    expect(pipe).toBeTruthy();
  });

  it('order', () =>{
    const testee: string = 'bcd';

    expect(pipe.transform(suggests, testee)).toEqual([b,a,c]);

  });



});
