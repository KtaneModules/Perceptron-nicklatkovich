from random import randint as r
from random import shuffle
from random import choice


DEBUG = False


def ceil100(x):
    return x // 100 + (1 if x % 100 > 0 else 0)


LAYERS_COUNT = 4

inputs = r(1, 4)
outputs = r(1, 4)

answer_example = [r(1, 4) for _ in range(LAYERS_COUNT)]

if DEBUG: print('answer_example: ' + str(answer_example))

ways_counts = [
    answer_example[0] * inputs,
    *[answer_example[i] * answer_example[i - 1] for i in range(1, LAYERS_COUNT)],
    answer_example[LAYERS_COUNT - 1] * outputs
]

ways_count_to_index = {}
for index, ways_count in enumerate(ways_counts):
	if ways_count not in ways_count_to_index: ways_count_to_index[ways_count] = []
	ways_count_to_index[ways_count].append(index)

if DEBUG: print('ways_count_to_index: ' + str(ways_count_to_index))

ways_count_sorted = ways_counts.copy()
ways_count_sorted.sort()
ways_count_sorted.reverse()

convergence_rates = [r(1, 999) for _ in range(LAYERS_COUNT + 1)]
convergence_rates.sort()
convergence_rates.reverse()

layers_properties = [None for _ in range(LAYERS_COUNT + 1)]

for i in range(LAYERS_COUNT + 1):
	ways_count = ways_count_sorted[i]
	ways_index = choice(ways_count_to_index[ways_count])
	ways_count_to_index[ways_count].remove(ways_index)
	layers_properties[ways_index] = { 'convergence_rate': convergence_rates[i], 'delay': r(1, 999) }

if DEBUG: print('layers_properties: ' + str(layers_properties))

answer_accuracy = sum([
	x // 100 for x in [ways_counts[i] * layers_properties[i]['convergence_rate'] for i in range(LAYERS_COUNT + 1)]
])

if DEBUG: print('answer_accuracy: ' + str(answer_accuracy))

answer_time = sum([
	ceil100(x) for x in [ways_counts[i] * layers_properties[i]['delay'] for i in range(LAYERS_COUNT + 1)]
])

if DEBUG: print('answer_time: ' + str(answer_time))

required_accuracy = answer_accuracy - r(0, answer_accuracy // 10)

mock_indicators_count = 0
for _ in range(0, r(5, 8)):
    if r(0, 3) == 0: mock_indicators_count += 1

mock_modules_count = r(2, 8)
mock_modules_count = mock_modules_count * r(mock_modules_count, mock_modules_count * 2) * 2 - 1

required_time = answer_time * mock_modules_count + mock_indicators_count * 300
required_time += r(0, required_time // 10)


print('indicators: ' + str(mock_indicators_count))
print('modules: ' + str(mock_modules_count))

print('inputs: ' + str(inputs))
print('outputs: ' + str(outputs))
print('accuracy: ' + str(required_accuracy))
print('time: ' + str(required_time) + ' ms')

for layer in layers_properties:
	print(str(layer['convergence_rate'] / 100) + ' / ' + str(layer['delay'] / 100))


lls = 0
lgs = 0
gls = 0
ggs = 0
for c1 in range(1, 5):
	for c2 in range(1, 5):
		for c3 in range(1, 5):
			for c4 in range(1, 5):
				acc = sum([x // 100 for x in [
					layers_properties[0]['convergence_rate'] * inputs * c1,
					layers_properties[1]['convergence_rate'] * c1 * c2,
					layers_properties[2]['convergence_rate'] * c2 * c3,
					layers_properties[3]['convergence_rate'] * c3 * c4,
					layers_properties[4]['convergence_rate'] * c4 * outputs,
				]])
				tm = sum([ceil100(x) for x in [
					layers_properties[0]['delay'] * inputs * c1,
					layers_properties[1]['delay'] * c1 * c2,
					layers_properties[2]['delay'] * c2 * c3,
					layers_properties[3]['delay'] * c3 * c4,
					layers_properties[4]['delay'] * c4 * outputs,
				]]) * mock_modules_count + mock_indicators_count * 300
				if acc < required_accuracy:
					if tm <= required_time: lls += 1
					else: lgs += 1
				elif tm <= required_time: gls += 1
				else: ggs += 1
print('lls: ' + str(lls) + ' (' + '{0:.3g}'.format(lls * 100 / 4 ** LAYERS_COUNT) + '%)')
print('lgs: ' + str(lgs) + ' (' + '{0:.3g}'.format(lgs * 100 / 4 ** LAYERS_COUNT) + '%)')
print('gls: ' + str(gls) + ' (' + '{0:.3g}'.format(gls * 100 / 4 ** LAYERS_COUNT) + '%)')
print('ggs: ' + str(ggs) + ' (' + '{0:.3g}'.format(ggs * 100 / 4 ** LAYERS_COUNT) + '%)')
