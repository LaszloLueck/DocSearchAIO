import {BytesVisualizerPipe} from "./BytesVisualizer.pipe";

describe('BytesVisualizerPipe', () => {
  const pipe = new BytesVisualizerPipe();

  it('generates', () => {
    expect(pipe).toBeTruthy();
  })

  it('transforms 100 to 100 B', () => {
    expect(pipe.transform(100)).toBe('100 B');
  })

  it('transforms 1050 to 1 kB', () => {
    expect(pipe.transform(1050)).toBe('1 kB');
  })

  it('transforms 1.048.576 to 1 MB', () => {
    expect(pipe.transform(1048576)).toBe('1 MB')
  })

  it('transforms 1.073.741.824 to 1 GB', () => {
    expect(pipe.transform(1073741824)).toBe('1 GB')
  });

  it('transforms not a word but returns it', () => {
    expect(pipe.transform('hurz')).toBe('hurz');
  })

})
