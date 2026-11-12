"""Generate fictional large Sysmac-style trace data; no equipment data is used."""
import argparse
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('output', type=Path)
parser.add_argument('--samples', type=int, default=200_000)
args = parser.parse_args()
with args.output.open('w', encoding='utf-8', newline='') as f:
    f.write('Index,Date,ClockTime,' + ','.join([f'Fictional.Bool{i}:BOOL' for i in range(50)] + [f'Fictional.Value{i}:INT' for i in range(20)]) + '\n')
    for row in range(args.samples):
        seconds, ms = divmod(row, 1000)
        minutes, seconds = divmod(seconds, 60)
        hours, minutes = divmod(minutes, 60)
        values = [str(((row + i * 17) // 1000) % 2) for i in range(50)]
        values += [str((row // 1000 + i) % 100) for i in range(20)]
        f.write(f'{row},2026/01/01,{hours:02}:{minutes:02}:{seconds:02}.{ms:03},' + ','.join(values) + '\n')
print(f'{args.output}: {args.samples} samples; 50 BOOL + 20 values; {args.output.stat().st_size} bytes')
